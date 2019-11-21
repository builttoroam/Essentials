using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        static TimeSpan defaultStartTimeFromNow = TimeSpan.Zero;

        static TimeSpan defaultEndTimeFromStartTime = TimeSpan.FromDays(14);

        public static bool IsSupported => PlatformIsSupported;

        public static void SetDefaultStartTimeOffset(TimeSpan newTimeOffset) => defaultStartTimeFromNow = newTimeOffset;

        public static void SetDefaultEndTimeOffset(TimeSpan newTimeOffset) => defaultEndTimeFromStartTime = newTimeOffset;

        public static Task RequestCalendarReadAccess() => PlatformRequestCalendarReadAccess();

        public static Task RequestCalendarWriteAccess() => PlatformRequestCalendarWriteAccess();

        public static Task<IReadOnlyList<DeviceCalendar>> GetCalendarsAsync() => PlatformGetCalendarsAsync();

        public static Task<IReadOnlyList<Event>> GetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null) => PlatformGetEventsAsync(calendarId, startDate, endDate);

        public static Task<Event> GetEventByIdAsync(string eventId) => PlatformGetEventByIdAsync(eventId);
    }
}
