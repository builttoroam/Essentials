using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Database;
using Android.Provider;
using Java.Security;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        const string andCondition = "AND";

        static async Task<IEnumerable<DeviceCalendar>> PlatformGetCalendarsAsync()
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var calendarsUri = CalendarContract.Calendars.ContentUri;
            var calendarsProjection = new List<string>
            {
                CalendarContract.Calendars.InterfaceConsts.Id,
                CalendarContract.Calendars.InterfaceConsts.CalendarAccessLevel,
                CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName
            };
            var queryConditions = $"{CalendarContract.Calendars.InterfaceConsts.Deleted} != 1";

            using (var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(calendarsUri, calendarsProjection.ToArray(), queryConditions, null, null))
            {
                var calendars = new List<DeviceCalendar>();
                while (cur.MoveToNext())
                {
                    calendars.Add(new DeviceCalendar()
                    {
                        Id = cur.GetString(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.Id)),
                        IsReadOnly = IsCalendarReadOnly((CalendarAccess)cur.GetInt(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.CalendarAccessLevel))),
                        Name = cur.GetString(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName)),
                    });
                }
                return calendars;
            }
        }

        static bool IsCalendarReadOnly(CalendarAccess accessLevel)
        {
            switch (accessLevel)
            {
                case CalendarAccess.AccessContributor:
                case CalendarAccess.AccessRoot:
                case CalendarAccess.AccessOwner:
                case CalendarAccess.AccessEditor:
                    return false;
                default:
                    return true;
            }
        }

        static async Task<IEnumerable<DeviceEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var eventsUri = CalendarContract.Events.ContentUri;
            var eventsProjection = new List<string>
            {
                CalendarContract.Events.InterfaceConsts.Id,
                CalendarContract.Events.InterfaceConsts.CalendarId,
                CalendarContract.Events.InterfaceConsts.Title,
                CalendarContract.Events.InterfaceConsts.Dtstart,
                CalendarContract.Events.InterfaceConsts.Dtend,
                CalendarContract.Events.InterfaceConsts.Deleted
            };
            var calendarSpecificEvent = string.Empty;
            var sDate = startDate ?? DateTimeOffset.Now.Add(defaultStartTimeFromNow);
            var eDate = endDate ?? sDate.Add(defaultEndTimeFromStartTime);
            if (!string.IsNullOrEmpty(calendarId))
            {
                // Android event ids are always integers
                if (!int.TryParse(calendarId, out var resultId))
                {
                    throw new ArgumentException($"[Android]: No Event found for event Id {calendarId}");
                }
                calendarSpecificEvent = $"{CalendarContract.Events.InterfaceConsts.CalendarId}={resultId} {andCondition} ";
            }
            calendarSpecificEvent += $"{CalendarContract.Events.InterfaceConsts.Dtend} >= {sDate.AddMilliseconds(sDate.Offset.TotalMilliseconds).ToUnixTimeMilliseconds()} {andCondition} ";
            calendarSpecificEvent += $"{CalendarContract.Events.InterfaceConsts.Dtstart} <= {eDate.AddMilliseconds(sDate.Offset.TotalMilliseconds).ToUnixTimeMilliseconds()} {andCondition} ";
            calendarSpecificEvent += $"{CalendarContract.Events.InterfaceConsts.Deleted} != 1";

            var events = new List<DeviceEvent>();
            using (var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(eventsUri, eventsProjection.ToArray(), calendarSpecificEvent, null, $"{CalendarContract.Events.InterfaceConsts.Dtstart} ASC"))
            {
                while (cur.MoveToNext())
                {
                    events.Add(new DeviceEvent()
                    {
                        Id = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Id)),
                        CalendarId = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.CalendarId)),
                        Title = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Title)),
                        StartDate = DateTimeOffset.FromUnixTimeMilliseconds(cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtstart))),
                        EndDate = DateTimeOffset.FromUnixTimeMilliseconds(cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtend)))
                    });
                }
            }
            if (events.Count == 0 && !string.IsNullOrEmpty(calendarId))
            {
                // Make sure this calendar exists by testing retrieval
                try
                {
                    GetCalendarById(calendarId);
                }
                catch (Exception)
                {
                    throw new ArgumentOutOfRangeException($"[Android]: No calendar exists with the Id {calendarId}");
                }
            }

            return events;
        }

        static DeviceCalendar GetCalendarById(string calendarId)
        {
            var calendarsUri = CalendarContract.Calendars.ContentUri;
            var calendarsProjection = new List<string>
            {
                CalendarContract.Calendars.InterfaceConsts.Id,
                CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName
            };

            // Android event ids are always integers
            if (!int.TryParse(calendarId, out var resultId))
            {
                throw new ArgumentException($"[Android]: No Event found for event Id {calendarId}");
            }

            var queryConditions = $"{CalendarContract.Calendars.InterfaceConsts.Deleted} != 1 {andCondition} {CalendarContract.Calendars.InterfaceConsts.Id} = {resultId}";

            using (var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(calendarsUri, calendarsProjection.ToArray(), queryConditions, null, null))
            {
                if (cur.Count > 0)
                {
                    cur.MoveToNext();
                    return new DeviceCalendar()
                    {
                        Id = cur.GetString(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.Id)),
                        Name = cur.GetString(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName)),
                    };
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"[Android]: No calendar exists with the Id {calendarId}");
                }
            }
        }

        static async Task<DeviceEvent> PlatformGetEventByIdAsync(string eventId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var eventsUri = CalendarContract.Events.ContentUri;
            var eventsProjection = new List<string>
            {
                CalendarContract.Events.InterfaceConsts.Id,
                CalendarContract.Events.InterfaceConsts.CalendarId,
                CalendarContract.Events.InterfaceConsts.Title,
                CalendarContract.Events.InterfaceConsts.Description,
                CalendarContract.Events.InterfaceConsts.EventLocation,
                CalendarContract.Events.InterfaceConsts.AllDay,
                CalendarContract.Events.InterfaceConsts.Dtstart,
                CalendarContract.Events.InterfaceConsts.Dtend
            };

            // Android event ids are always integers
            if (!int.TryParse(eventId, out var resultId))
            {
                throw new ArgumentException($"[Android]: No Event found for event Id {eventId}");
            }

            var calendarSpecificEvent = $"{CalendarContract.Events.InterfaceConsts.Id}={resultId}";
            using (var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(eventsUri, eventsProjection.ToArray(), calendarSpecificEvent, null, null))
            {
                if (cur.Count > 0)
                {
                    cur.MoveToNext();
                    var eventResult = new DeviceEvent
                    {
                        Id = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Id)),
                        CalendarId = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.CalendarId)),
                        Title = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Title)),
                        Description = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Description)),
                        Location = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventLocation)),
                        StartDate = DateTimeOffset.FromUnixTimeMilliseconds(cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtstart))),
                        EndDate = cur.GetInt(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.AllDay)) == 0 ? (DateTimeOffset?)DateTimeOffset.FromUnixTimeMilliseconds(cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtend))) : null,
                        Attendees = GetAttendeesForEvent(eventId)
                    };
                    return eventResult;
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"[Android]: No Event found for event Id {eventId}");
                }
            }
        }

        static IEnumerable<DeviceEventAttendee> GetAttendeesForEvent(string eventId)
        {
            var attendeesUri = CalendarContract.Attendees.ContentUri;
            var attendeesProjection = new List<string>
            {
                CalendarContract.Attendees.InterfaceConsts.EventId,
                CalendarContract.Attendees.InterfaceConsts.AttendeeEmail,
                CalendarContract.Attendees.InterfaceConsts.AttendeeName
            };
            var attendeeSpecificAttendees = $"{CalendarContract.Attendees.InterfaceConsts.EventId}={eventId}";
            var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(attendeesUri, attendeesProjection.ToArray(), attendeeSpecificAttendees, null, null);
            var attendees = new List<DeviceEventAttendee>();
            while (cur.MoveToNext())
            {
                attendees.Add(new DeviceEventAttendee()
                {
                    Name = cur.GetString(attendeesProjection.IndexOf(CalendarContract.Attendees.InterfaceConsts.AttendeeName)),
                    Email = cur.GetString(attendeesProjection.IndexOf(CalendarContract.Attendees.InterfaceConsts.AttendeeEmail)),
                });
            }
            cur.Dispose();
            return attendees;
        }

        static async Task<string> PlatformCreateCalendar(DeviceCalendar newCalendar)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var calendarUri = CalendarContract.Calendars.ContentUri;
            var cursor = Platform.AppContext.ApplicationContext.ContentResolver;
            var calendarValues = new ContentValues();
            calendarValues.Put(CalendarContract.Calendars.Name, newCalendar.Name);
            calendarValues.Put(CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName, newCalendar.Name);
            var result = cursor.Insert(calendarUri, calendarValues);
            return result.ToString();
        }

        static async Task<string> PlatformCreateCalendarEvent(DeviceEvent newEvent)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var result = 0;
            if (string.IsNullOrEmpty(newEvent.CalendarId))
            {
                return string.Empty;
            }
            var eventUri = CalendarContract.Events.ContentUri;
            var eventValues = new ContentValues();

            eventValues.Put(CalendarContract.Events.InterfaceConsts.CalendarId, newEvent.CalendarId);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Title, newEvent.Title);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Description, newEvent.Description);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.EventLocation, newEvent.Location);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.AllDay, newEvent.AllDay);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Dtstart, newEvent.StartDate.ToUnixTimeMilliseconds().ToString());
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Dtend, newEvent.EndDate.HasValue ? newEvent.EndDate.Value.ToUnixTimeMilliseconds().ToString() : newEvent.StartDate.AddDays(1).ToString());
            eventValues.Put(CalendarContract.Events.InterfaceConsts.EventTimezone, TimeZoneInfo.Local.Id);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Deleted, 0);

            try
            {
                var resultUri = Platform.AppContext.ApplicationContext.ContentResolver.Insert(eventUri, eventValues);
                result = Convert.ToInt32(resultUri.LastPathSegment);
            }
            catch (NullReferenceException ex)
            {
                throw ex;
            }

            return result.ToString();
        }

        static async Task<bool> PlatformDeleteCalendarEventById(string eventId, string calendarId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentException("[Android]: You must supply an event id to delete an event.");
            }

            var calendarEvent = await GetEventByIdAsync(eventId);

            if (calendarEvent.CalendarId != calendarId)
            {
                throw new ArgumentOutOfRangeException("[Android]: Supplied event does not belong to supplied calendar");
            }

            var eventUri = ContentUris.WithAppendedId(CalendarContract.Events.ContentUri, long.Parse(eventId));
            var result = Platform.AppContext.ApplicationContext.ContentResolver.Delete(eventUri, null, null);

            return result > 0;
        }

        static Task<bool> PlatformAddAttendeeToEvent(DeviceEventAttendee newAttendee, string eventId) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<bool> PlatformRemoveAttendeeFromEvent(DeviceEventAttendee newAttendee, string eventId) => throw ExceptionUtils.NotSupportedOrImplementedException;
    }
}
