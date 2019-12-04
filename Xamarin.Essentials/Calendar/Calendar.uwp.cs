﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        static async Task PlatformRequestCalendarReadAccess() => await Permissions.RequireAsync(PermissionType.CalendarRead);

        static async Task PlatformRequestCalendarWriteAccess() => await Permissions.RequireAsync(PermissionType.CalendarWrite);

        static async Task<List<DeviceCalendar>> PlatformGetCalendarsAsync()
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var instance = await CalendarRequest.GetInstanceAsync();
            var uwpCalendarList = await instance.FindAppointmentCalendarsAsync(FindAppointmentCalendarsOptions.IncludeHidden);
            var calendars = new List<DeviceCalendar>();
            foreach (var c in uwpCalendarList)
            {
                calendars.Add(new DeviceCalendar()
                {
                    Id = c.LocalId,
                    Name = c.DisplayName,
                    IsReadOnly = c.OtherAppWriteAccess != AppointmentCalendarOtherAppWriteAccess.Limited
                });
            }
            return calendars;
        }

        static async Task<List<DeviceEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var options = new FindAppointmentsOptions();
            options.FetchProperties.Add(AppointmentProperties.Subject);
            options.FetchProperties.Add(AppointmentProperties.StartTime);
            options.FetchProperties.Add(AppointmentProperties.Duration);
            var sDate = startDate ?? DateTimeOffset.Now.Add(defaultStartTimeFromNow);
            var eDate = endDate ?? sDate.Add(defaultEndTimeFromStartTime);

            if (eDate < sDate)
                eDate = sDate;

            var instance = await CalendarRequest.GetInstanceAsync();
            var events = await instance.FindAppointmentsAsync(sDate, eDate.Subtract(sDate), options);
            var eventList = new List<DeviceEvent>();
            foreach (var e in events)
            {
                if (calendarId == null || e.CalendarId == calendarId)
                {
                    eventList.Add(new DeviceEvent
                    {
                        Id = e.LocalId,
                        CalendarId = e.CalendarId,
                        Title = e.Subject,
                        StartDate = e.StartTime,
                        EndDate = e.StartTime.Add(e.Duration)
                    });
                }
            }
            eventList.Sort((x, y) =>
            {
                if (!y.EndDate.HasValue)
                {
                    return 0;
                }
                return x.StartDate.CompareTo(y.EndDate.Value);
            });

            return eventList;
        }

        static async Task<DeviceEvent> PlatformGetEventByIdAsync(string eventId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var instance = await CalendarRequest.GetInstanceAsync();

            Appointment e;
            try
            {
                e = await instance.GetAppointmentAsync(eventId);
                e.DetailsKind = AppointmentDetailsKind.PlainText;
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"[UWP]: No Event found for event Id {eventId}");
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException($"[UWP]: No Event found for event Id {eventId}");
            }

            return new DeviceEvent()
            {
                Id = e.LocalId,
                CalendarId = e.CalendarId,
                Title = e.Subject,
                Description = e.Details,
                Location = e.Location,
                StartDate = e.StartTime,
                EndDate = e.StartTime.Add(e.Duration),
                AllDay = e.AllDay,
                Attendees = GetAttendeesForEvent(e.Invitees)
            };
        }

        static List<DeviceEventAttendee> GetAttendeesForEvent(IList<AppointmentInvitee> inviteList)
        {
            var attendees = new List<DeviceEventAttendee>();

            foreach (var attendee in inviteList)
            {
                attendees.Add(new DeviceEventAttendee()
                {
                    Name = attendee.DisplayName,
                    Email = attendee.Address
                });
            }
            return attendees;
        }
    }
}
