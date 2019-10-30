using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using Android.Content;
using Android.Database;
using Android.Provider;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        public static bool PlatformIsSupported => true;

        public static async Task<List<CalendarObject>> PlatformGetCalendarsAsync()
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);
            var calendars = new List<CalendarObject>();
            var calendarsUri = CalendarContract.Calendars.ContentUri;
            string[] calendarsProjection =
            {
                CalendarContract.Calendars.InterfaceConsts.Id,
                CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName,
                CalendarContract.Calendars.InterfaceConsts.CalendarAccessLevel
            };
            if (Platform.AppContext == null)
                return await Task.FromResult(new List<CalendarObject>() { new CalendarObject() { Name = "Could not retrieve list" } });

            var ctx = Platform.AppContext;
            var cur = ctx.ApplicationContext.ContentResolver.Query(calendarsUri, calendarsProjection, null, null, null);

            while (cur.MoveToNext())
            {
                calendars.Add(new CalendarObject()
                {
                    Id = cur.GetString(0), // Id
                    Name = cur.GetString(1), // CalendarDisplayName
                    IsReadOnly = cur.GetString(2) == "Owner" // CalendarAccessLevel
                });
            }

            return await Task.FromResult(calendars);
        }

        public static async Task PlatformRequestCalendarReadAccess() => await Permissions.RequireAsync(PermissionType.CalendarRead);

        public static async Task PlatformRequestCalendarWriteAccess() => await Permissions.RequireAsync(PermissionType.CalendarWrite);
    }
}
