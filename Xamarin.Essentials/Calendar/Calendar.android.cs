﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        public static bool PlatformIsSupported => true;

        public static Task<List<CalendarObject>> PlatformGetCalendarsAsync()
        {
            return Task.FromResult(new List<CalendarObject>()
            {
                new CalendarObject
                {
                    Id = "1",
                    Name = "My First Calendar",
                    IsReadOnly = false
                }
            });
        }

        public static async Task PlatformRequestCalendarReadAccess() => await Permissions.RequireAsync(PermissionType.CalendarRead);

        public static async Task PlatformRequestCalendarWriteAccess() => await Permissions.RequireAsync(PermissionType.CalendarWrite);
    }
}
