using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Provider;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        public static bool PlatformIsSupported => true;

        public static async Task<IReadOnlyList<ICalendar>> PlatformGetCalendarsAsync()
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var calendarsUri = CalendarContract.Calendars.ContentUri;
            var calendarsProjection = new List<string>
            {
                CalendarContract.Calendars.InterfaceConsts.Id,
                CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName,
                CalendarContract.Calendars.InterfaceConsts.CalendarAccessLevel
            };

            var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(calendarsUri, calendarsProjection.ToArray(), null, null, null);
            var calendars = new List<ICalendar>();
            while (cur.MoveToNext())
            {
                calendars.Add(new DeviceCalendar()
                {
                    Id = cur.GetString(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.Id)),
                    Name = cur.GetString(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName)),
                    IsReadOnly = cur.GetString(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.CalendarAccessLevel)) == "Owner"
                });
            }

            return calendars.AsReadOnly();
        }

        public static async Task PlatformRequestCalendarReadAccess() => await Permissions.RequireAsync(PermissionType.CalendarRead);

        public static async Task PlatformRequestCalendarWriteAccess() => await Permissions.RequireAsync(PermissionType.CalendarWrite);
    }
}
