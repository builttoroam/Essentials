using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        public static bool IsSupported => PlatformIsSupported;

        public static Task<IReadOnlyList<ICalendar>> GetCalendarsAsync() => PlatformGetCalendarsAsync();

        public static Task<IReadOnlyList<IEvent>> GetEventsAsync(string calendarId = "") => PlatformGetEventsAsync(calendarId);

        public static Task RequestCalendarReadAccess() => PlatformRequestCalendarReadAccess();

        public static Task RequestCalendarWriteAccess() => PlatformRequestCalendarWriteAccess();
    }
}
