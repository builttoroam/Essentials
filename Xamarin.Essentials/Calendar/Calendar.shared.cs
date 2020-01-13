using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        static TimeSpan defaultStartTimeFromNow = TimeSpan.Zero;

        static TimeSpan defaultEndTimeFromStartTime = TimeSpan.FromDays(14);

        public static Task<IEnumerable<DeviceCalendar>> GetCalendarsAsync() => PlatformGetCalendarsAsync();

        public static Task<IEnumerable<DeviceEvent>> GetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null) => PlatformGetEventsAsync(calendarId, startDate, endDate);

        public static Task<DeviceEvent> GetEventByIdAsync(string eventId) => PlatformGetEventByIdAsync(eventId);

        public static Task<DeviceEvent> GetEventInstanceByIdAsync(string eventId, DateTimeOffset instanceDate) => PlatformGetEventInstanceByIdAsync(eventId, instanceDate);

        public static Task<string> CreateCalendar(DeviceCalendar newCalendar) => PlatformCreateCalendar(newCalendar);

        public static Task<string> CreateCalendarEvent(DeviceEvent newEvent) => PlatformCreateCalendarEvent(newEvent);

        public static Task<bool> UpdateCalendarEvent(DeviceEvent newEvent) => PlatformUpdateCalendarEvent(newEvent);

        public static Task<bool> DeleteCalendarEventInstanceByDate(string eventId, string calendarId, DateTimeOffset dateOfInstanceUtc) => PlatformDeleteCalendarEventInstanceByDate(eventId, calendarId, dateOfInstanceUtc);

        public static Task<bool> DeleteCalendarEventById(string eventId, string calendarId) => PlatformDeleteCalendarEventById(eventId, calendarId);

        public static Task<bool> AddAttendeeToEvent(DeviceEventAttendee newAttendee, string eventId) => PlatformAddAttendeeToEvent(newAttendee, eventId);

        public static Task<bool> RemoveAttendeeFromEvent(DeviceEventAttendee newAttendee, string eventId) => PlatformRemoveAttendeeFromEvent(newAttendee, eventId);
    }
}
