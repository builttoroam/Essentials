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
            var rules = new RecurrenceRule();
            rules.Interval = e.Recurrence.Interval;
            rules.TotalOccurences = e.Recurrence.Occurrences;
            rules.EndDate = e.Recurrence.Until;
            rules.DaysOfTheWeek = ConvertBitFlagToIntList((int)e.Recurrence.DaysOfWeek, (int)AppointmentDaysOfWeek.Saturday).Select(x => (DayOfTheWeek)x + 1).ToList();
            rules.Frequency = (RecurrenceFrequency)e.Recurrence.Unit;
            return new DeviceEvent()
            {
                Id = e.LocalId,
                CalendarId = e.CalendarId,
                Title = e.Subject,
                Description = e.Details,
                Location = e.Location,
                StartDate = e.StartTime,
                EndDate = !e.AllDay ? (DateTimeOffset?)e.StartTime.Add(e.Duration) : null,
                Attendees = GetAttendeesForEvent(e.Invitees),
                RecurrancePattern = rules
            };
        }

        static List<int> ConvertBitFlagToIntList(int wholeNumber, int maxValue)
        {
            var currentVal = wholeNumber;
            var toReturn = new List<int>();
            for (var i = maxValue; i > 0; i /= 2)
            {
                if (currentVal >= i)
                {
                    toReturn.Add((int)Math.Log(i, 2));
                    currentVal -= i;
                }
            }
            return toReturn;
        }

        static IEnumerable<DeviceEventAttendee> GetAttendeesForEvent(IEnumerable<AppointmentInvitee> inviteList)
        {
            var attendees = (from attendee in inviteList
                             select new DeviceEventAttendee
                             {
                                 Name = attendee.DisplayName,
                                 Email = attendee.Address,
                                 Required = attendee.Role == AppointmentParticipantRole.RequiredAttendee
                             })
                            .OrderBy(e => e.Name)
                            .ToList();

            return attendees;
        }

        static async Task<string> PlatformCreateCalendarEvent(DeviceEvent newEvent)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            if (string.IsNullOrEmpty(newEvent.CalendarId))
            {
                return string.Empty;
            }

            var instance = await CalendarRequest.GetInstanceAsync();

            var app = new Appointment();
            app.Subject = newEvent.Title;
            app.Details = newEvent.Description;
            app.Location = newEvent.Location;
            app.StartTime = newEvent.StartDate;
            app.Duration = newEvent.EndDate.HasValue ? newEvent.EndDate.Value - newEvent.StartDate : TimeSpan.FromDays(1);
            app.AllDay = newEvent.AllDay;

            var cal = await instance.GetAppointmentCalendarAsync(newEvent.CalendarId);
            await cal.SaveAppointmentAsync(app);

            if (!string.IsNullOrEmpty(app.LocalId))
                return app.LocalId;

            throw new ArgumentException("[UWP]: Could not create appointment with supplied parameters");
        }

        static async Task<bool> PlatformUpdateCalendarEvent(DeviceEvent eventToUpdate)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var existingEvent = await GetEventByIdAsync(eventToUpdate.Id);

            Appointment thisEvent = null;
            var instance = await CalendarRequest.GetInstanceAsync();
            if (string.IsNullOrEmpty(eventToUpdate.CalendarId) || existingEvent == null)
            {
                return false;
            }
            else if (existingEvent.CalendarId != eventToUpdate.CalendarId)
            {
                await DeleteCalendarEventById(existingEvent.Id, existingEvent.CalendarId);
                thisEvent = new Appointment();
            }
            else
            {
                thisEvent = await instance.GetAppointmentAsync(eventToUpdate.Id);
            }

            thisEvent.Subject = eventToUpdate.Title;
            thisEvent.Details = eventToUpdate.Description;
            thisEvent.Location = eventToUpdate.Location;
            thisEvent.StartTime = eventToUpdate.StartDate;
            thisEvent.Duration = eventToUpdate.EndDate.HasValue ? eventToUpdate.EndDate.Value - eventToUpdate.StartDate : TimeSpan.FromDays(1);
            thisEvent.AllDay = eventToUpdate.AllDay;

            var cal = await instance.GetAppointmentCalendarAsync(eventToUpdate.CalendarId);
            await cal.SaveAppointmentAsync(thisEvent);

            if (!string.IsNullOrEmpty(thisEvent.LocalId))
            {
                return true;
            }
            throw new ArgumentException("[UWP]: Could not update appointment with supplied parameters");
        }

        static async Task<string> PlatformCreateCalendar(DeviceCalendar newCalendar)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var instance = await CalendarRequest.GetInstanceAsync();

            var cal = await instance.CreateAppointmentCalendarAsync(newCalendar.Name);

            if (cal != null)
                return cal.LocalId;

            throw new ArgumentException("[UWP]: Could not create appointment with supplied parameters");
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

            var calendarEvent = await instance.GetAppointmentAsync(eventId);
            var cal = await instance.GetAppointmentCalendarAsync(calendarEvent.CalendarId);
            var cntInvitiees = calendarEvent.Invitees.Count;

            if (calendarEvent == null)
                throw new ArgumentException("[UWP]: You must supply a valid event id to add an attendee to.");

            calendarEvent.Invitees.Add(new AppointmentInvitee() { DisplayName = newAttendee.Name, Address = newAttendee.Email, Role = newAttendee.Required ? AppointmentParticipantRole.RequiredAttendee : AppointmentParticipantRole.OptionalAttendee });
            await cal.SaveAppointmentAsync(calendarEvent);

            return calendarEvent.Invitees.Count == cntInvitiees + 1;
        }

        static async Task<bool> PlatformRemoveAttendeeFromEvent(DeviceEventAttendee newAttendee, string eventId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var instance = await CalendarRequest.GetInstanceAsync();

            var calendarEvent = await instance.GetAppointmentAsync(eventId);
            var cal = await instance.GetAppointmentCalendarAsync(calendarEvent.CalendarId);

            if (calendarEvent == null)
                throw new ArgumentException("[UWP]: You must supply a valid event id to remove an attendee from.");

            var attendeeToRemove = calendarEvent.Invitees.Where(x => x.DisplayName == newAttendee.Name && x.Address == newAttendee.Email).FirstOrDefault();

            calendarEvent.Invitees.Remove(attendeeToRemove);

            await cal.SaveAppointmentAsync(calendarEvent);

            return attendeeToRemove != null;
        }
    }
}
