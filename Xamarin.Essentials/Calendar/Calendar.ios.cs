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

        public static Task<List<CalendarObject>> PlatformGetCalendarsAsync()
        {
            var calendars = CalendarRequest.Instance.Calendars;
            var toReturn = new List<CalendarObject>();

            foreach (var c in calendars)
            {
                toReturn.Add(new CalendarObject
                {
                    Id = c.CalendarIdentifier,
                    Name = c.Title,
                    IsReadOnly = !c.AllowsContentModifications
                });
            }
            return Task.FromResult(toReturn);
        }

        public static async Task PlatformRequestCalendarReadAccess() => await Permissions.RequireAsync(PermissionType.CalendarRead);

        public static async Task PlatformRequestCalendarWriteAccess() => await Permissions.RequireAsync(PermissionType.CalendarWrite);
    }
}
