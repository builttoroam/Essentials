using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        static bool PlatformIsSupported => true;

        static async Task<IReadOnlyList<ICalendar>> PlatformGetCalendarsAsync()
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var calendars = CalendarRequest.Instance.Calendars;
            var calendarList = new List<DeviceCalendar>();

            foreach (var t in calendars)
            {
                calendarList.Add(new DeviceCalendar
                {
                    Id = t.CalendarIdentifier,
                    Name = t.Title,
                    IsReadOnly = !t.AllowsContentModifications
                });
            }
            return calendarList.AsReadOnly();
        }

        static Task<IReadOnlyList<IEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null) => throw new NotImplementedException();

        static async Task PlatformRequestCalendarReadAccess()
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);
        }

        static async Task PlatformRequestCalendarWriteAccess()
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);
        }

        static async Task<PermissionStatus> PlatformCheckCalendarReadAccess() => await Permissions.CheckStatusAsync(PermissionType.CalendarRead);

        static async Task<PermissionStatus> PlatformCheckCalendarWriteAccess() => await Permissions.CheckStatusAsync(PermissionType.CalendarWrite);

        static Task<int> PlatformCreateCalendarEvent(IEvent newEvent) => throw new NotImplementedException();
    }
}
