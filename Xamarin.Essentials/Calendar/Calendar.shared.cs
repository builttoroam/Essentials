using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        static TimeSpan defaultDateDistance = TimeSpan.FromDays(14);

        public static void SetDefaultDateDistance(TimeSpan newDefaultSpan) => defaultDateDistance = newDefaultSpan;

        public static bool IsSupported => PlatformIsSupported;

        public static Task<IReadOnlyList<ICalendar>> GetCalendarsAsync() => PlatformGetCalendarsAsync();

        public static Task<IReadOnlyList<IEvent>> GetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null) => PlatformGetEventsAsync(calendarId, startDate, endDate);

        public static Task<IEvent> GetEventByIdAsync(string eventId) => PlatformGetEventByIdAsync(eventId);

        public static Task RequestCalendarReadAccess() => PlatformRequestCalendarReadAccess();

        public static Task RequestCalendarWriteAccess() => PlatformRequestCalendarWriteAccess();
    }
}
