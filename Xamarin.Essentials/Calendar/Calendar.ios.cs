using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventKit;
using Foundation;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        static bool PlatformIsSupported => true;

        static async Task<IReadOnlyList<ICalendar>> PlatformGetCalendarsAsync()
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            EKCalendar[] calendars;
            try
            {
                calendars = CalendarRequest.Instance.Calendars;
            }
            catch (NullReferenceException ex)
            {
                throw new Exception($"iOS: Unexpected null reference exception {ex.Message}");
            }
            var calendarList = new List<DeviceCalendar>();

            foreach (var t in calendars)
            {
                calendarList.Add(new DeviceCalendar
                {
                    Id = t.CalendarIdentifier,
                    Name = t.Title,
                    IsReadOnly = !t.AllowsContentModifications
                });
            }
            return calendarList.AsReadOnly();
        }

        static async Task<IReadOnlyList<IEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var eventList = new List<Event>();
            var startDateToConvert = startDate ?? DateTimeOffset.Now.Add(defaultStartTimeFromNow);
            var endDateToConvert = endDate ?? startDateToConvert.Add(defaultEndTimeFromStartTime);  // NOTE: 4 years is the maximum period that a iOS calendar events can search
            var sDate = startDateToConvert.ToNSDate();
            var eDate = endDateToConvert.ToNSDate();
            EKCalendar[] calendars;
            try
            {
                calendars = !string.IsNullOrWhiteSpace(calendarId)
                    ? CalendarRequest.Instance.Calendars.Where(x => x.CalendarIdentifier == calendarId).ToArray()
                    : null;
            }
            catch (NullReferenceException ex)
            {
                throw new Exception($"iOS: Unexpected null reference exception {ex.Message}");
            }

            var query = CalendarRequest.Instance.PredicateForEvents(sDate, eDate, calendars);
            var events = CalendarRequest.Instance.EventsMatching(query);

            foreach (var e in events)
            {
                eventList.Add(new Event
                {
                    Id = e.CalendarItemIdentifier,
                    CalendarId = e.Calendar.CalendarIdentifier,
                    Title = e.Title,
                    Start = e.StartDate.ToEpochTime(),
                    End = e.EndDate.ToEpochTime()
                });
            }
            eventList.Sort((x, y) =>
            {
                if (!x.StartDate.HasValue)
                {
                    if (!y.EndDate.HasValue)
                    {
                        return 0;
                    }
                    return -1;
                }
                if (!y.EndDate.HasValue)
                {
                    return 1;
                }
                return x.StartDate.Value.CompareTo(y.EndDate.Value);
            });

            return eventList.AsReadOnly();
        }

        static async Task PlatformRequestCalendarReadAccess() => await Permissions.RequireAsync(PermissionType.CalendarRead);

        static async Task PlatformRequestCalendarWriteAccess() => await Permissions.RequireAsync(PermissionType.CalendarWrite);

        static async Task<IEvent> PlatformGetEventByIdAsync(string eventId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            EKEvent e;
            try
            {
                e = CalendarRequest.Instance.GetCalendarItem(eventId) as EKEvent;
            }
            catch (NullReferenceException ex)
            {
                throw new Exception($"iOS: Unexpected null reference exception {ex.Message}");
            }

            return new Event
            {
                Id = e.CalendarItemIdentifier,
                Title = e.Title,
                Description = e.Notes,
                Location = e.Location,
                Start = e.StartDate.ToEpochTime(),
                End = e.EndDate.ToEpochTime(),
                Attendees = e.Attendees != null ? GetAttendeesForEvent(e.Attendees) : new List<IAttendee>()
            };
        }

        static IReadOnlyList<IAttendee> GetAttendeesForEvent(IList<EKParticipant> inviteList)
        {
            var attendees = new List<IAttendee>();

            foreach (var attendee in inviteList)
            {
                attendees.Add(new Attendee()
                {
                    Name = attendee.Name,
                    Email = attendee.Name
                });
            }
            return attendees.AsReadOnly();
        }

        static async Task<bool> PlatformDeleteCalendarEventById(string eventId, string calendarId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentException("[iOS]: You must supply an event id to delete an event.");
            }

            var calendarEvent = CalendarRequest.Instance.GetCalendarItem(eventId) as EKEvent;

            if (calendarEvent.Calendar.CalendarIdentifier != calendarId)
            {
                throw new ArgumentOutOfRangeException("[iOS]: Supplied event does not belong to supplied calendar.");
            }

            if (CalendarRequest.Instance.RemoveEvent(calendarEvent, EKSpan.ThisEvent, true, out var error))
            {
                return true;
            }
            throw new Exception(error.DebugDescription);
        }

        static async Task<string> PlatformCreateCalendar(DeviceCalendar newCalendar)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var calendar = EKCalendar.Create(EKEntityType.Event, CalendarRequest.Instance);

            if (CalendarRequest.Instance.SaveCalendar(calendar, true, out var error))
            {
                return calendar.CalendarIdentifier;
            }
            throw new Exception(error.DebugDescription);
        }

        static Task<bool> PlatformAddAttendeeToEvent(DeviceEventAttendee newAttendee, string eventId) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<bool> PlatformRemoveAttendeeFromEvent(DeviceEventAttendee newAttendee, string eventId) => throw ExceptionUtils.NotSupportedOrImplementedException;
    }
}
