using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    }
}
