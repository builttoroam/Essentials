﻿using System;
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
                calendarSpecificEvent = $"{CalendarContract.Events.InterfaceConsts.CalendarId}={calendarId} {andCondition} ";
            }
            calendarSpecificEvent += $"{CalendarContract.Events.InterfaceConsts.Dtend} >= {sDate.AddMilliseconds(sDate.Offset.TotalMilliseconds).ToUnixTimeMilliseconds()} {andCondition} ";
            calendarSpecificEvent += $"{CalendarContract.Events.InterfaceConsts.Dtstart} <= {eDate.AddMilliseconds(sDate.Offset.TotalMilliseconds).ToUnixTimeMilliseconds()} {andCondition} ";
            calendarSpecificEvent += $"{CalendarContract.Events.InterfaceConsts.Deleted} != 1";

            using (var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(eventsUri, eventsProjection.ToArray(), calendarSpecificEvent, null, $"{CalendarContract.Events.InterfaceConsts.Dtstart} ASC"))
            {
                var events = new List<DeviceEvent>();
                while (cur.MoveToNext())
                {
                    events.Add(new DeviceEvent()
                    {
                        Id = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Id)),
                        CalendarId = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.CalendarId)),
                        Title = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Title)),
                        StartDate = DateTimeOffset.FromUnixTimeMilliseconds(cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtstart))),
                        EndDate = cur.GetInt(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.AllDay)) == 0 ? (DateTimeOffset?)DateTimeOffset.FromUnixTimeMilliseconds(cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtend))) : null
                    });
                }
                return events;
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
                    throw new ArgumentException($"[Android]: No Event found for event Id {eventId}");
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
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Dtend, newEvent.EndDate.ToUnixTimeMilliseconds().ToString());
            eventValues.Put(CalendarContract.Events.InterfaceConsts.EventTimezone, TimeZoneInfo.Local.Id);

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
    }
}
