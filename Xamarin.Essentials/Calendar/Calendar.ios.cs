using System.Collections.Generic;
using System.Threading.Tasks;
using EventKit;
using Foundation;
using UIKit;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        public static bool PlatformIsSupported => true;

        public static async Task<IReadOnlyList<ICalendar>> PlatformGetCalendarsAsync()
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
                /*
                 To get Events:
                 var mySavedEvent = CalendarRequest.Instance.EventFromIdentifier(t.CalendarIdentifier);
                */
            }
            return calendarList.AsReadOnly();
        }

        public static async Task PlatformRequestCalendarReadAccess()
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);
            await Permissions.RequireAsync(PermissionType.CalendarWrite);
        }

        public static async Task PlatformRequestCalendarWriteAccess()
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);
            await Permissions.RequireAsync(PermissionType.CalendarWrite);
        }
    }
}
