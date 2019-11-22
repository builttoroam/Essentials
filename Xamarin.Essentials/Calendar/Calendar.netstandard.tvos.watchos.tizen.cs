using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        static bool PlatformIsSupported => false;

        static Task PlatformRequestCalendarReadAccess() => throw new NotImplementedException();

        static Task PlatformRequestCalendarWriteAccess() => throw new NotImplementedException();

        static Task<List<DeviceCalendar>> PlatformGetCalendarsAsync() => throw new NotImplementedException();

        static Task<List<DeviceEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null) => throw new NotImplementedException();

        static Task<DeviceEvent> PlatformGetEventByIdAsync(string eventId) => throw new NotImplementedException();
    }
}
