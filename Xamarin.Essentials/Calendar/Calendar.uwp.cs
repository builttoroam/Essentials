using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        static bool PlatformIsSupported => true;

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

        static async Task<List<Event>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
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
                        StartDate = e.StartTime,
                        EndDate = e.StartTime.Add(e.Duration)
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

            return eventList;
        }

        static async Task<Event> PlatformGetEventByIdAsync(string eventId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var instance = await CalendarRequest.GetInstanceAsync();

            Appointment e;
            try
            {
                e = await instance.GetAppointmentAsync(eventId);
                e.DetailsKind = AppointmentDetailsKind.PlainText;
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException($"[UWP]: No Event found for event Id {eventId}");
            }

            return new Event()
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

        static List<Attendee> GetAttendeesForEvent(IList<AppointmentInvitee> inviteList)
        {
            var attendees = new List<Attendee>();

            foreach (var attendee in inviteList)
            {
                attendees.Add(new Attendee()
                {
                    Name = attendee.DisplayName,
                    Email = attendee.Address
                });
            }
            return attendees;
        }
    }
}
