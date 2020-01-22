using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
    public static partial class Calendars
    {
        static TimeSpan defaultStartTimeFromNow = TimeSpan.Zero;

        static TimeSpan defaultEndTimeFromStartTime = TimeSpan.FromDays(14);

        public static Task<IEnumerable<Calendar>> GetCalendarsAsync() => PlatformGetCalendarsAsync();

        public static Task<IEnumerable<CalendarEvent>> GetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null) => PlatformGetEventsAsync(calendarId, startDate, endDate);

        public static Task<CalendarEvent> GetEventByIdAsync(string eventId) => PlatformGetEventByIdAsync(eventId);

        public static Task<CalendarEvent> GetEventInstanceByIdAsync(string eventId, DateTimeOffset instanceDate) => PlatformGetEventInstanceByIdAsync(eventId, instanceDate);

        public static Task<string> CreateCalendarEvent(CalendarEvent newEvent) => PlatformCreateCalendarEvent(newEvent);

        public static Task<bool> UpdateCalendarEvent(CalendarEvent eventToUpdate) => PlatformUpdateCalendarEvent(eventToUpdate);

        public static Task<bool> DeleteCalendarEventInstanceByDate(string eventId, string calendarId, DateTimeOffset dateOfInstanceUtc) => PlatformDeleteCalendarEventInstanceByDate(eventId, calendarId, dateOfInstanceUtc);

        public static Task<bool> DeleteCalendarEventById(string eventId, string calendarId) => PlatformDeleteCalendarEventById(eventId, calendarId);

        public static Task<string> CreateCalendar(Calendar newCalendar) => PlatformCreateCalendar(newCalendar);

        public static Task<bool> AddAttendeeToEvent(CalendarEventAttendee newAttendee, string eventId) => PlatformAddAttendeeToEvent(newAttendee, eventId);

        public static Task<bool> RemoveAttendeeFromEvent(CalendarEventAttendee newAttendee, string eventId) => PlatformRemoveAttendeeFromEvent(newAttendee, eventId);
    }
}
