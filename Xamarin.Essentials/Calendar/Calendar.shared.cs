using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        public static bool IsSupported => PlatformIsSupported;

        public static Task<IReadOnlyList<ICalendar>> GetCalendarsAsync() => PlatformGetCalendarsAsync();

        public static Task<IReadOnlyList<IEvent>> GetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null) => PlatformGetEventsAsync(calendarId, startDate, endDate);

        public static Task RequestCalendarReadAccess() => PlatformRequestCalendarReadAccess();

        public static Task RequestCalendarWriteAccess() => PlatformRequestCalendarWriteAccess();

        public static Task<string> CreateCalendarEvent(IEvent newEvent) => PlatformCreateCalendarEvent(newEvent);

        static TimeSpan defaultEndTimeFromStartTime = TimeSpan.FromDays(14);

        static TimeSpan defaultStartTimeFromNow = TimeSpan.Zero;

        public static void SetDefaultStartTimeFromNow(TimeSpan newDefaultSpan) => defaultStartTimeFromNow = newDefaultSpan;

        public static void SetDefaultEndTimeFromStartTime(TimeSpan newDefaultSpan) => defaultEndTimeFromStartTime = newDefaultSpan;

        public static Task<IEvent> GetEventByIdAsync(string eventId) => PlatformGetEventByIdAsync(eventId);
    }
}
