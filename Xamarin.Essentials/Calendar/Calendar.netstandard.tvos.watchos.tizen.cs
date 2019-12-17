﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        static Task<IEnumerable<DeviceCalendar>> PlatformGetCalendarsAsync() => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<IEnumerable<DeviceEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<DeviceEvent> PlatformGetEventByIdAsync(string eventId) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<string> PlatformCreateCalendar(DeviceCalendar newCalendar) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<string> PlatformCreateOrUpdateCalendarEvent(DeviceEvent newEvent) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<bool> PlatformDeleteCalendarEventById(string eventId, string calendarId) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<bool> PlatformAddAttendeeToEvent(DeviceEventAttendee newAttendee, string eventId) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<bool> PlatformRemoveAttendeeFromEvent(DeviceEventAttendee newAttendee, string eventId) => throw ExceptionUtils.NotSupportedOrImplementedException;
    }
}
