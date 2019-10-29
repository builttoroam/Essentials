using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        public static bool PlatformIsSupported => false;

        public static Task<List<CalendarObject>> PlatformGetCalendarsAsync() => throw new NotImplementedException();

        public static Task PlatformRequestCalendarReadAccess() => throw new NotImplementedException();

        public static Task PlatformRequestCalendarWriteAccess() => throw new NotImplementedException();
    }
}
