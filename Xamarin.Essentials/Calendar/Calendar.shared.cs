using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        // public static List<Calendar> CalendarList { get; set; }
        public static bool IsSupported()
            => PlatformIsSupported;

        public static Task<IReadOnlyList<ICalendar>> GetCalendarsAsync()
            => PlatformGetCalendarsAsync();

        public static Task RequestCalendarReadAccess()
            => PlatformRequestCalendarReadAccess();

        public static Task RequestCalendarWriteAccess()
            => PlatformRequestCalendarWriteAccess();
    }
}
