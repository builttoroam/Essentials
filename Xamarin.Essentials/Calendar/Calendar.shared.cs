﻿using System;
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

        public static Task<List<DeviceCalendar>> GetCalendarsAsync() => PlatformGetCalendarsAsync();

        public static Task<List<DeviceEvent>> GetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null) => PlatformGetEventsAsync(calendarId, startDate, endDate);

        public static Task<DeviceEvent> GetEventByIdAsync(string eventId) => PlatformGetEventByIdAsync(eventId);
    }
}
