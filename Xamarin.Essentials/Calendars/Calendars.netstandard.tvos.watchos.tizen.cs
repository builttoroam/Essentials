using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
    public static partial class Calendars
    {
        static Task<IEnumerable<Calendar>> PlatformGetCalendarsAsync() => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<IEnumerable<CalendarEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<CalendarEvent> PlatformGetEventByIdAsync(string eventId) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<CalendarEvent> PlatformGetEventInstanceByIdAsync(string eventId, DateTimeOffset instanceDate) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<string> PlatformCreateCalendarEvent(CalendarEvent newEvent) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<bool> PlatformUpdateCalendarEvent(CalendarEvent eventToUpdate) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<bool> PlatformSetEventRecurrenceEndDate(string eventId, DateTimeOffset recurrenceEndDate) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<bool> PlatformDeleteCalendarEventInstanceByDate(string eventId, string calendarId, DateTimeOffset dateOfInstanceUtc) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<bool> PlatformDeleteCalendarEventById(string eventId, string calendarId) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<string> PlatformCreateCalendar(Calendar newCalendar) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<bool> PlatformAddAttendeeToEvent(CalendarEventAttendee newAttendee, string eventId) => throw ExceptionUtils.NotSupportedOrImplementedException;

        static Task<bool> PlatformRemoveAttendeeFromEvent(CalendarEventAttendee newAttendee, string eventId) => throw ExceptionUtils.NotSupportedOrImplementedException;
    }
}
