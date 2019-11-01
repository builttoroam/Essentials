﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Provider;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        static bool PlatformIsSupported => true;

        static async Task<IReadOnlyList<ICalendar>> PlatformGetCalendarsAsync()
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
                    IsReadOnly = IsCalendarReadOnly((CalendarAccess)cur.GetInt(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.CalendarAccessLevel)))
                });
            }

            return calendars.AsReadOnly();
        }

        static bool IsCalendarReadOnly(CalendarAccess accessLevel)
        {
            switch (accessLevel)
            {
                case CalendarAccess.AccessContributor:
                case CalendarAccess.AccessRoot:
                case CalendarAccess.AccessOwner:
                case CalendarAccess.AccessEditor:
                    return false;
                default:
                    return true;
            }
        }

        static async Task PlatformRequestCalendarReadAccess() => await Permissions.RequireAsync(PermissionType.CalendarRead);

        static async Task PlatformRequestCalendarWriteAccess() => await Permissions.RequireAsync(PermissionType.CalendarWrite);

        public static async Task<IReadOnlyList<IEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var eventsUri = CalendarContract.Events.ContentUri;
            var eventsProjection = new List<string>
            {
                CalendarContract.Events.InterfaceConsts.Id,
                CalendarContract.Events.InterfaceConsts.CalendarId,
                CalendarContract.Events.InterfaceConsts.Title,
                CalendarContract.Events.InterfaceConsts.Description,
                CalendarContract.Events.InterfaceConsts.EventLocation,
                CalendarContract.Events.InterfaceConsts.AllDay,
                CalendarContract.Events.InterfaceConsts.Dtstart,
                CalendarContract.Events.InterfaceConsts.Dtend,
                CalendarContract.Events.InterfaceConsts.Duration,
                CalendarContract.Events.InterfaceConsts.HasAlarm,
                CalendarContract.Events.InterfaceConsts.HasAttendeeData,
                CalendarContract.Events.InterfaceConsts.HasExtendedProperties,
                CalendarContract.Events.InterfaceConsts.Status,
                CalendarContract.Events.InterfaceConsts.Rrule,
                CalendarContract.Events.InterfaceConsts.Rdate
            };
            var calendarSpecificEvent = string.Empty;
            if (!string.IsNullOrEmpty(calendarId))
            {
                calendarSpecificEvent = $"{CalendarContract.Events.InterfaceConsts.CalendarId}={calendarId} AND ";
            }
            if (startDate != null)
            {
                calendarSpecificEvent += $"{CalendarContract.Events.InterfaceConsts.Dtstart} >= {startDate.Value.ToUnixTimeMilliseconds()} AND ";
            }
            if (endDate != null)
            {
                calendarSpecificEvent += $"{CalendarContract.Events.InterfaceConsts.Dtend} <= {endDate.Value.ToUnixTimeMilliseconds()} AND ";
            }

            calendarSpecificEvent += "deleted != 1";

            var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(eventsUri, eventsProjection.ToArray(), calendarSpecificEvent, null, null);
            var events = new List<IEvent>();
            while (cur.MoveToNext())
            {
                events.Add(new Event()
                {
                    Id = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Id)),
                    CalendarId = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.CalendarId)),
                    Title = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Title)),
                    Description = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Description)),
                    Location = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventLocation)),
                    AllDay = cur.GetInt(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.AllDay)) == 1,
                    Start = cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtstart)),
                    End = cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtend)),
                    HasAlarm = cur.GetInt(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.HasAlarm)) == 1,
                    HasAttendees = cur.GetInt(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.HasAttendeeData)) == 1,
                    HasExtendedProperties = cur.GetInt(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.HasExtendedProperties)) == 1,
                    Status = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Status)),
                    RecurrenceFrequency = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Rrule)),
                    RecurranceDate = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Rdate)),
                });
            }

            return events.AsReadOnly();
        }
    }
}
