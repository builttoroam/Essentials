﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        static bool PlatformIsSupported => true;

        static async Task<IReadOnlyList<ICalendar>> PlatformGetCalendarsAsync()
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);
            var appointmentStore = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AllCalendarsReadWrite);
            var uwpCalendarList = await appointmentStore.FindAppointmentCalendarsAsync(FindAppointmentCalendarsOptions.IncludeHidden);
            var calendars = new List<ICalendar>();
            foreach (var c in uwpCalendarList)
            {
                calendars.Add(new DeviceCalendar()
                {
                    Id = c.LocalId,
                    Name = c.DisplayName,
                    IsReadOnly = c.OtherAppWriteAccess != AppointmentCalendarOtherAppWriteAccess.Limited
                });
            }
            return calendars.AsReadOnly();
        }

        static async Task<IReadOnlyList<IEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var appointmentStore = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AllCalendarsReadWrite);

            var options = new FindAppointmentsOptions();
            options.FetchProperties.Add(AppointmentProperties.Subject);
            options.FetchProperties.Add(AppointmentProperties.StartTime);
            options.FetchProperties.Add(AppointmentProperties.Duration);
            var sDate = startDate ?? DateTimeOffset.Now;
            var eDate = endDate ?? sDate.AddYears(4);

            if (eDate < sDate)
                eDate = sDate;

            var events = await appointmentStore.FindAppointmentsAsync(sDate, eDate.Subtract(sDate), options);
            var eventList = new List<Event>();
            foreach (var e in events)
            {
                if (calendarId == null || e.CalendarId == calendarId)
                {
                    eventList.Add(new Event
                    {
                        Id = e.LocalId,
                        CalendarId = e.CalendarId,
                        Title = e.Subject,
                        Start = e.StartTime.ToUnixTimeMilliseconds(),
                        End = e.StartTime.Add(e.Duration).ToUnixTimeMilliseconds()
                    });
                }
            }
            eventList.Sort((x, y) =>
            {
                if (!x.StartDate.HasValue)
                {
                    if (!y.EndDate.HasValue)
                    {
                        return 0;
                    }
                    return -1;
                }
                return !y.EndDate.HasValue ? 1 : x.StartDate.Value.CompareTo(y.EndDate.Value);
            });

            return eventList.AsReadOnly();
        }

        static async Task<IEvent> PlatformGetEventByIdAsync(string eventId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var appointmentStore = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AllCalendarsReadWrite);

            var e = await appointmentStore.GetAppointmentAsync(eventId);

            return new Event()
            {
                Id = e.LocalId,
                CalendarId = e.CalendarId,
                Title = e.Subject,
                Description = e.Details,
                Location = e.Location,
                Start = e.StartTime.ToUnixTimeMilliseconds(),
                End = e.StartTime.Add(e.Duration).ToUnixTimeMilliseconds(),
                Attendees = GetAttendeesForEvent(e.Invitees)
            };
        }

        static async Task PlatformRequestCalendarReadAccess() => await Permissions.RequireAsync(PermissionType.CalendarRead);

        static async Task PlatformRequestCalendarWriteAccess() => await Permissions.RequireAsync(PermissionType.CalendarWrite);

        static IReadOnlyList<IAttendee> GetAttendeesForEvent(IList<AppointmentInvitee> inviteList)
        {
            var attendees = new List<IAttendee>();

            foreach (var attendee in inviteList)
            {
                attendees.Add(new Attendee()
                {
                    Name = attendee.DisplayName,
                    Email = attendee.Address
                });
            }
            return attendees.AsReadOnly();
        }
}
}