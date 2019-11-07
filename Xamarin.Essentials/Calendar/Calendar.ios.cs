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

            var calendars = CalendarRequest.Instance.Calendars;
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

            var systemAbsoluteReferenceDate = new DateTime(2001, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var eventList = new List<Event>();
            var startDateToConvert = startDate ?? DateTimeOffset.Now;
            var endDateToConvert = endDate ?? startDateToConvert.AddYears(4);   // 4 years is the maximum period that a iOS calendar events can search
            var sDate = NSDate.FromTimeIntervalSinceReferenceDate((startDateToConvert.UtcDateTime - systemAbsoluteReferenceDate).TotalSeconds);
            var eDate = NSDate.FromTimeIntervalSinceReferenceDate((endDateToConvert.UtcDateTime - systemAbsoluteReferenceDate).TotalSeconds);
            var calendars = !string.IsNullOrEmpty(calendarId) ? CalendarRequest.Instance.Calendars.Where(x => x.CalendarIdentifier == calendarId).ToArray() : null;
            var query = CalendarRequest.Instance.PredicateForEvents(sDate, eDate, calendars);
            var events = CalendarRequest.Instance.EventsMatching(query);

            foreach (var e in events)
            {
                eventList.Add(new Event
                {
                    Id = e.CalendarItemIdentifier,
                    CalendarId = e.Calendar.CalendarIdentifier,
                    Title = e.Title,
                    Start = (long)Math.Floor((Math.Abs(NSDate.FromTimeIntervalSince1970(0).SecondsSinceReferenceDate) + e.StartDate.SecondsSinceReferenceDate) * 1000),
                    End = (long)Math.Floor((Math.Abs(NSDate.FromTimeIntervalSince1970(0).SecondsSinceReferenceDate) + e.EndDate.SecondsSinceReferenceDate) * 1000)
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
                return !y.EndDate.HasValue ? 1 : x.StartDate.Value.CompareTo(y.EndDate.Value);
            });

            return eventList.AsReadOnly();
        }

        static async Task PlatformRequestCalendarReadAccess() => await Permissions.RequireAsync(PermissionType.CalendarRead);

        static async Task PlatformRequestCalendarWriteAccess() => await Permissions.RequireAsync(PermissionType.CalendarWrite);

        static async Task<IEvent> PlatformGetEventByIdAsync(string eventId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var e = CalendarRequest.Instance.GetCalendarItem(eventId) as EKEvent;

            return new Event
            {
                Id = e.CalendarItemIdentifier,
                Title = e.Title,
                Description = e.Notes,
                Location = e.Location,
                Start = (long)Math.Floor((Math.Abs(NSDate.FromTimeIntervalSince1970(0).SecondsSinceReferenceDate) + e.StartDate.SecondsSinceReferenceDate) * 1000),
                End = (long)Math.Floor((Math.Abs(NSDate.FromTimeIntervalSince1970(0).SecondsSinceReferenceDate) + e.EndDate.SecondsSinceReferenceDate) * 1000),
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
    }
}
