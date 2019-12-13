using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        static async Task<IEnumerable<DeviceCalendar>> PlatformGetCalendarsAsync()
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var instance = await CalendarRequest.GetInstanceAsync(AppointmentStoreAccessType.AllCalendarsReadWrite);
            var uwpCalendarList = await instance.FindAppointmentCalendarsAsync(FindAppointmentCalendarsOptions.IncludeHidden);

            var calendars = (from calendar in uwpCalendarList
                                select new DeviceCalendar
                                {
                                    Id = calendar.LocalId,
                                    Name = calendar.DisplayName,

                                        // This logic seems reversed but I'm unsure why, this actually works as expected.
                                    IsReadOnly = calendar.CanCreateOrUpdateAppointments
                                }).ToList();

            return calendars;
        }

        static async Task<IEnumerable<DeviceEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var options = new FindAppointmentsOptions();
            options.FetchProperties.Add(AppointmentProperties.Subject);
            options.FetchProperties.Add(AppointmentProperties.StartTime);
            options.FetchProperties.Add(AppointmentProperties.Duration);
            options.FetchProperties.Add(AppointmentProperties.AllDay);
            var sDate = startDate ?? DateTimeOffset.Now.Add(defaultStartTimeFromNow);
            var eDate = endDate ?? sDate.Add(defaultEndTimeFromStartTime);

            if (eDate < sDate)
                eDate = sDate;

            var instance = await CalendarRequest.GetInstanceAsync(AppointmentStoreAccessType.AllCalendarsReadOnly);
            var events = await instance.FindAppointmentsAsync(sDate, eDate.Subtract(sDate), options);

            var eventList = (from e in events
                             select new DeviceEvent
                             {
                                 Id = e.LocalId,
                                 CalendarId = e.CalendarId,
                                 Title = e.Subject,
                                 StartDate = e.StartTime,
                                 EndDate = !e.AllDay ? (DateTimeOffset?)e.StartTime.Add(e.Duration) : null
                             })
                            .Where(e => e.CalendarId == calendarId || calendarId == null)
                            .OrderBy(e => e.StartDate)
                            .ToList();

            if (eventList.Count == 0 && !string.IsNullOrWhiteSpace(calendarId))
            {
                await GetCalendarById(calendarId);
            }

            return eventList;
        }

        static async Task<DeviceCalendar> GetCalendarById(string calendarId)
        {
            var instance = await CalendarRequest.GetInstanceAsync(AppointmentStoreAccessType.AllCalendarsReadOnly);
            var uwpCalendarList = await instance.FindAppointmentCalendarsAsync(FindAppointmentCalendarsOptions.IncludeHidden);

            var result = (from calendar in uwpCalendarList
                             select new DeviceCalendar
                             {
                                 Id = calendar.LocalId,
                                 Name = calendar.DisplayName
                             })
                             .Where(c => c.Id == calendarId).FirstOrDefault();
            if (result == null)
            {
                throw new ArgumentOutOfRangeException($"[UWP]: No calendar exists with the Id {calendarId}");
            }

            return result;
        }

        static async Task<DeviceEvent> PlatformGetEventByIdAsync(string eventId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var instance = await CalendarRequest.GetInstanceAsync(AppointmentStoreAccessType.AllCalendarsReadOnly);

            Appointment e;
            try
            {
                e = await instance.GetAppointmentAsync(eventId);
                e.DetailsKind = AppointmentDetailsKind.PlainText;
            }
            catch (ArgumentException)
            {
                if (string.IsNullOrWhiteSpace(eventId))
                {
                    throw new ArgumentException($"[UWP]: No Event found for event Id {eventId}");
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"[UWP]: No Event found for event Id {eventId}");
                }
            }

            return new DeviceEvent()
            {
                Id = e.LocalId,
                CalendarId = e.CalendarId,
                Title = e.Subject,
                Description = e.Details,
                Location = e.Location,
                StartDate = e.StartTime,
                EndDate = !e.AllDay ? (DateTimeOffset?)e.StartTime.Add(e.Duration) : null,
                Attendees = GetAttendeesForEvent(e.Invitees)
            };
        }

        static IEnumerable<DeviceEventAttendee> GetAttendeesForEvent(IEnumerable<AppointmentInvitee> inviteList)
        {
            var attendees = (from attendee in inviteList
                             select new DeviceEventAttendee
                             {
                                 Name = attendee.DisplayName,
                                 Email = attendee.Address
                             })
                            .OrderBy(e => e.Name)
                            .ToList();

            return attendees;
        }

        static async Task<string> PlatformCreateCalendarEvent(DeviceEvent newEvent)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var instance = await CalendarRequest.GetInstanceAsync();

            var app = new Appointment()
            {
                Subject = newEvent.Title,
                Details = newEvent.Description,
                Location = newEvent.Location,
                StartTime = newEvent.StartDate,
                Duration = newEvent.EndDate.HasValue ? newEvent.EndDate.Value - newEvent.StartDate : TimeSpan.FromDays(1),
                AllDay = newEvent.AllDay
            };
            try
            {
                // ShowAddAppointmentAsync might be the way to deal with uwp, but it requires a UI Windows.Foundation.Rect object which may be hard to access within here
                var cal = await instance.GetAppointmentCalendarAsync(newEvent.CalendarId);
                await cal.SaveAppointmentAsync(app);

                if (!string.IsNullOrEmpty(app.LocalId))
                    return app.LocalId;

                throw new ArgumentException("[UWP]: Could not create appointment with supplied parameters");
            }
            catch (NullReferenceException ex)
            {
                throw ex;
            }
        }

        static async Task<string> PlatformCreateCalendar(DeviceCalendar newCalendar)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var instance = await CalendarRequest.GetInstanceAsync();

            try
            {
                // var appointmentStore = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);
                var cal = await instance.CreateAppointmentCalendarAsync(newCalendar.Name);

                if (cal != null)
                    return cal.LocalId;

                throw new ArgumentException("[UWP]: Could not create appointment with supplied parameters");
            }
            catch (NullReferenceException ex)
            {
                throw ex;
            }
        }

        static async Task<bool> PlatformDeleteCalendarEventById(string eventId, string calendarId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);
            var instance = await CalendarRequest.GetInstanceAsync();

            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentException("[Android]: You must supply an event id to delete an event.");
            }
            var calendarEvent = await GetEventByIdAsync(eventId);

            if (calendarEvent.CalendarId != calendarId)
            {
                throw new ArgumentOutOfRangeException("[Android]: Supplied event does not belong to supplied calendar");
            }

            var cal = await instance.GetAppointmentCalendarAsync(calendarId);
            var app = await instance.GetAppointmentAsync(eventId);
            app.IsCanceledMeeting = true;
            await cal.SaveAppointmentAsync(app);

            if (app.IsCanceledMeeting)
                return true;

            return false;
        }

        static async Task<bool> PlatformAddAttendeeToEvent(DeviceEventAttendee newAttendee, string eventId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var instance = await CalendarRequest.GetInstanceAsync();

            try
            {
                var calendarEvent = await instance.GetAppointmentAsync(eventId);
                var cal = await instance.GetAppointmentCalendarAsync(calendarEvent.CalendarId);
                var cntInvitiees = calendarEvent.Invitees.Count;

                if (calendarEvent == null)
                    return false;

                calendarEvent.Invitees.Add(new AppointmentInvitee() { DisplayName = newAttendee.Name, Address = newAttendee.Email });

                await cal.SaveAppointmentAsync(calendarEvent);
                if (calendarEvent.Invitees.Count == cntInvitiees + 1)
                    return true;

                throw new ArgumentException("[UWP]: Could not create attendee for event with supplied parameters");
            }
            catch (NullReferenceException ex)
            {
                throw ex;
            }
        }

        static async Task<bool> PlatformRemoveAttendeeFromEvent(DeviceEventAttendee newAttendee, string eventId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var instance = await CalendarRequest.GetInstanceAsync();

            try
            {
                var calendarEvent = await instance.GetAppointmentAsync(eventId);
                var cal = await instance.GetAppointmentCalendarAsync(calendarEvent.CalendarId);
                var cntInvitiees = calendarEvent.Invitees.Count;

                if (calendarEvent == null)
                    return false;

                var attendeeToRemove = calendarEvent.Invitees.Where(x => x.DisplayName == newAttendee.Name && x.Address == newAttendee.Email).First();

                calendarEvent.Invitees.Remove(attendeeToRemove);

                await cal.SaveAppointmentAsync(calendarEvent);
                if (calendarEvent.Invitees.Count == cntInvitiees - 1)
                    return true;

                throw new ArgumentException("[UWP]: Could not remove attendee from event with supplied parameters");
            }
            catch (NullReferenceException ex)
            {
                throw ex;
            }
        }
    }
}
