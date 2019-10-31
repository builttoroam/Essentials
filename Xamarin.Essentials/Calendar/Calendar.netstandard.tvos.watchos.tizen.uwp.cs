using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        static bool PlatformIsSupported => false;

        static Task<IReadOnlyList<ICalendar>> PlatformGetCalendarsAsync() => throw new NotImplementedException();

        static Task PlatformRequestCalendarReadAccess() => throw new NotImplementedException();

        static Task PlatformRequestCalendarWriteAccess() => throw new NotImplementedException();
    }
}
