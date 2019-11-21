using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Database;
using Android.Provider;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        const string andCondition = "AND";

        static bool PlatformIsSupported => true;

        static async Task PlatformRequestCalendarReadAccess() => await Permissions.RequireAsync(PermissionType.CalendarRead);

        static async Task PlatformRequestCalendarWriteAccess() => await Permissions.RequireAsync(PermissionType.CalendarWrite);

        static async Task<IReadOnlyList<ICalendar>> PlatformGetCalendarsAsync()
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var calendarsUri = CalendarContract.Calendars.ContentUri;
            var calendarsProjection = new List<string>
            {
                CalendarContract.Calendars.InterfaceConsts.Id,
                CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName,
                CalendarContract.Calendars.InterfaceConsts.CalendarAccessLevel,
                CalendarContract.Calendars.InterfaceConsts.Deleted
            };

            var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(calendarsUri, calendarsProjection.ToArray(), null, null, null);
            var calendars = new List<ICalendar>();
            while (cur.MoveToNext())
            {
                calendars.Add(new DeviceCalendar()
                {
                    Id = cur.GetString(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.Id)),
                    Name = cur.GetString(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName)),
                    IsReadOnly = IsCalendarReadOnly((CalendarAccess)cur.GetInt(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.CalendarAccessLevel))),
                    Deleted = cur.GetInt(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.Deleted)) == 1
                });
            }
            cur.Dispose();
            return calendars.AsReadOnly();
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

        static async Task<IReadOnlyList<IEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
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
                calendarSpecificEvent = $"{CalendarContract.Events.InterfaceConsts.CalendarId}={calendarId} {andCondition} ";
            }
            calendarSpecificEvent += $"{CalendarContract.Events.InterfaceConsts.Dtstart} >= {sDate.ToUnixTimeMilliseconds()} {andCondition} ";
            calendarSpecificEvent += $"{CalendarContract.Events.InterfaceConsts.Dtend} <= {eDate.ToUnixTimeMilliseconds()}";

            var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(eventsUri, eventsProjection.ToArray(), calendarSpecificEvent, null, $"{CalendarContract.Events.InterfaceConsts.Dtstart} ASC");
            var events = new List<IEvent>();
            while (cur.MoveToNext())
            {
                events.Add(new Event()
                {
                    Id = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Id)),
                    CalendarId = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.CalendarId)),
                    Title = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Title)),
                    Start = cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtstart)),
                    End = cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtend)),
                    Deleted = cur.GetInt(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Deleted)) == 1
                });
            }
            cur.Dispose();
            return events.AsReadOnly();
        }

        static async Task<IEvent> PlatformGetEventByIdAsync(string eventId)
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
                CalendarContract.Events.InterfaceConsts.Dtend,
                CalendarContract.Events.InterfaceConsts.Deleted
            };
            var calendarSpecificEvent = $"{CalendarContract.Events.InterfaceConsts.Id}={eventId}";
            try
            {
                using (var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(eventsUri, eventsProjection.ToArray(), calendarSpecificEvent, null, null))
                {
                    var rRule = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Rrule));
                    var eventResult = new Event
                    {
                        Id = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Id)),
                        CalendarId = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.CalendarId)),
                        Title = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Title)),
                        Description = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Description)),
                        Location = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventLocation)),
                        AllDay = cur.GetInt(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.AllDay)) == 1,
                        Start = cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtstart)),
                        End = cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtend)),
                        Attendees = GetAttendeesForEvent(eventId),
                        Deleted = cur.GetInt(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Deleted)) == 1,
                    };
                    return eventResult;
                }
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException($"[Android]: No Event found for event Id {eventId}");
            }
        }

        static IReadOnlyList<IAttendee> GetAttendeesForEvent(string eventId)
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
            var attendees = new List<IAttendee>();
            while (cur.MoveToNext())
            {
                attendees.Add(new Attendee()
                {
                    Name = cur.GetString(attendeesProjection.IndexOf(CalendarContract.Attendees.InterfaceConsts.AttendeeName)),
                    Email = cur.GetString(attendeesProjection.IndexOf(CalendarContract.Attendees.InterfaceConsts.AttendeeEmail)),
                });
            }
            cur.Dispose();
            return attendees.AsReadOnly();
        }
    }
}
