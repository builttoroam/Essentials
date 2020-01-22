using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;

namespace Xamarin.Essentials
{
    public static partial class Calendars
    {
        static async Task<IEnumerable<Calendar>> PlatformGetCalendarsAsync()
        {
            await Permissions.RequestAsync<Permissions.CalendarRead>();

            var instance = await CalendarRequest.GetInstanceAsync(AppointmentStoreAccessType.AllCalendarsReadWrite);
            var uwpCalendarList = await instance.FindAppointmentCalendarsAsync(FindAppointmentCalendarsOptions.IncludeHidden);

            var calendars = (from calendar in uwpCalendarList
                                select new Calendar
                                {
                                    Id = calendar.LocalId,
                                    Name = calendar.DisplayName,

                                        // This logic seems reversed but I'm unsure why, this actually works as expected.
                                    IsReadOnly = calendar.CanCreateOrUpdateAppointments
                                }).ToList();

            return calendars;
        }

        static async Task<IEnumerable<CalendarEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            await Permissions.RequestAsync<Permissions.CalendarRead>();

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
                             select new CalendarEvent
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

        static async Task<Calendar> GetCalendarById(string calendarId)
        {
            var instance = await CalendarRequest.GetInstanceAsync(AppointmentStoreAccessType.AllCalendarsReadOnly);
            var uwpCalendarList = await instance.FindAppointmentCalendarsAsync(FindAppointmentCalendarsOptions.IncludeHidden);

            var result = (from calendar in uwpCalendarList
                             select new Calendar
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

        static async Task<CalendarEvent> PlatformGetEventByIdAsync(string eventId)
        {
            await Permissions.RequestAsync<Permissions.CalendarRead>();

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
            RecurrenceRule rules = null;
            if (e.Recurrence != null)
            {
                rules = new RecurrenceRule();
                rules.Frequency = (RecurrenceFrequency)e.Recurrence.Unit;
                rules.Interval = e.Recurrence.Interval;
                rules.EndDate = e.Recurrence.Until;
                rules.TotalOccurrences = e.Recurrence.Occurrences;
                switch (rules.Frequency)
                {
                    case RecurrenceFrequency.Daily:
                    case RecurrenceFrequency.Weekly:
                        rules.DaysOfTheWeek = ConvertBitFlagToIntList((int)e.Recurrence.DaysOfWeek, (int)AppointmentDaysOfWeek.Saturday).Select(x => (DayOfTheWeek)x + 1).ToList();
                        break;
                    case RecurrenceFrequency.MonthlyOnDay:
                        rules.WeekOfMonth = (IterationOffset)e.Recurrence.WeekOfMonth;
                        rules.DaysOfTheWeek = ConvertBitFlagToIntList((int)e.Recurrence.DaysOfWeek, (int)AppointmentDaysOfWeek.Saturday).Select(x => (DayOfTheWeek)x + 1).ToList();
                        break;
                    case RecurrenceFrequency.Monthly:
                        rules.DayOfTheMonth = e.Recurrence.Day;
                        break;
                    case RecurrenceFrequency.YearlyOnDay:
                        rules.WeekOfMonth = (IterationOffset)e.Recurrence.WeekOfMonth;
                        rules.DaysOfTheWeek = ConvertBitFlagToIntList((int)e.Recurrence.DaysOfWeek, (int)AppointmentDaysOfWeek.Saturday).Select(x => (DayOfTheWeek)x + 1).ToList();
                        break;
                    case RecurrenceFrequency.Yearly:
                        rules.DayOfTheMonth = e.Recurrence.Day;
                        rules.MonthOfTheYear = (MonthOfYear)e.Recurrence.Month;
                        break;
                }
            }

            return new CalendarEvent()
            {
                Id = e.LocalId,
                CalendarId = e.CalendarId,
                Title = e.Subject,
                Description = e.Details,
                Location = e.Location,
                Url = e.Uri != null ? e.Uri.ToString() : string.Empty,
                StartDate = e.StartTime,
                EndDate = !e.AllDay ? (DateTimeOffset?)e.StartTime.Add(e.Duration) : null,
                Attendees = GetAttendeesForEvent(e.Invitees, e.Organizer),
                RecurrancePattern = rules,
                Reminders = e.Reminder.HasValue ? new List<CalendarEventReminder>() { new CalendarEventReminder() { MinutesPriorToEventStart = e.Reminder.Value.Minutes } } : null
            };
        }

        static async Task<CalendarEvent> PlatformGetEventInstanceByIdAsync(string eventId, DateTimeOffset instanceDate)
        {
            await Permissions.RequestAsync<Permissions.CalendarRead>();

            var instance = await CalendarRequest.GetInstanceAsync(AppointmentStoreAccessType.AllCalendarsReadOnly);

            Appointment e;
            try
            {
                e = await instance.GetAppointmentInstanceAsync(eventId, instanceDate);
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
            RecurrenceRule rules = null;
            if (e.Recurrence != null)
            {
                rules = new RecurrenceRule();
                rules.Frequency = (RecurrenceFrequency)e.Recurrence.Unit;
                rules.Interval = e.Recurrence.Interval;
                rules.EndDate = e.Recurrence.Until;
                rules.TotalOccurrences = e.Recurrence.Occurrences;
                switch (rules.Frequency)
                {
                    case RecurrenceFrequency.Daily:
                    case RecurrenceFrequency.Weekly:
                        rules.DaysOfTheWeek = ConvertBitFlagToIntList((int)e.Recurrence.DaysOfWeek, (int)AppointmentDaysOfWeek.Saturday).Select(x => (DayOfTheWeek)x + 1).ToList();
                        break;
                    case RecurrenceFrequency.MonthlyOnDay:
                        rules.WeekOfMonth = (IterationOffset)e.Recurrence.WeekOfMonth;
                        rules.DaysOfTheWeek = ConvertBitFlagToIntList((int)e.Recurrence.DaysOfWeek, (int)AppointmentDaysOfWeek.Saturday).Select(x => (DayOfTheWeek)x + 1).ToList();
                        break;
                    case RecurrenceFrequency.Monthly:
                        rules.DayOfTheMonth = e.Recurrence.Day;
                        break;
                    case RecurrenceFrequency.YearlyOnDay:
                        rules.WeekOfMonth = (IterationOffset)e.Recurrence.WeekOfMonth;
                        rules.DaysOfTheWeek = ConvertBitFlagToIntList((int)e.Recurrence.DaysOfWeek, (int)AppointmentDaysOfWeek.Saturday).Select(x => (DayOfTheWeek)x + 1).ToList();
                        rules.MonthOfTheYear = (MonthOfYear)e.Recurrence.Month;
                        break;
                    case RecurrenceFrequency.Yearly:
                        rules.DayOfTheMonth = e.Recurrence.Day;
                        rules.MonthOfTheYear = (MonthOfYear)e.Recurrence.Month;
                        break;
                }
            }

            return new CalendarEvent()
            {
                Id = e.LocalId,
                CalendarId = e.CalendarId,
                Title = e.Subject,
                Description = e.Details,
                Location = e.Location,
                Url = e.Uri != null ? e.Uri.ToString() : string.Empty,
                StartDate = e.StartTime,
                EndDate = !e.AllDay ? (DateTimeOffset?)e.StartTime.Add(e.Duration) : null,
                Attendees = GetAttendeesForEvent(e.Invitees, e.Organizer),
                RecurrancePattern = rules,
                Reminders = e.Reminder.HasValue ? new List<CalendarEventReminder>() { new CalendarEventReminder() { MinutesPriorToEventStart = e.Reminder.Value.Minutes } } : null
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

        static int ConvertIntListToBitFlag(List<int> listOfNumbers)
        {
            var toReturn = 0;
            foreach (var i in listOfNumbers)
            {
                toReturn += (int)GetEnumByIndex(i);
            }
            return toReturn;
        }

        public static AppointmentDaysOfWeek GetEnumByIndex(int index)
        {
            var enums = Enum.GetValues(typeof(AppointmentDaysOfWeek));
            return (AppointmentDaysOfWeek)enums.GetValue(index);
        }

        static IEnumerable<CalendarEventAttendee> GetAttendeesForEvent(IEnumerable<AppointmentInvitee> inviteList, AppointmentOrganizer organizer)
        {
            var attendees = (from attendee in inviteList
                             select new CalendarEventAttendee
                             {
                                 Name = attendee.DisplayName,
                                 Email = attendee.Address,
                                 Type = (AttendeeType)attendee.Role + 1
                             })
                            .OrderBy(e => e.Name)
                            .ToList();
            if (organizer != null)
            {
                attendees.Insert(0, new CalendarEventAttendee() { Name = organizer.DisplayName, Email = organizer.Address, IsOrganizer = true });
            }

            return attendees;
        }

        static async Task<string> PlatformCreateCalendarEvent(CalendarEvent newEvent)
        {
            await Permissions.RequestAsync<Permissions.CalendarWrite>();

            if (string.IsNullOrEmpty(newEvent.CalendarId))
            {
                return string.Empty;
            }

            var instance = await CalendarRequest.GetInstanceAsync();

            var app = new Appointment();
            app.Subject = newEvent.Title;
            app.Details = newEvent.Description ?? string.Empty;
            app.Location = newEvent.Location ?? string.Empty;
            app.StartTime = newEvent.StartDate;
            app.Duration = newEvent.EndDate.HasValue ? newEvent.EndDate.Value - newEvent.StartDate : TimeSpan.FromDays(1);
            app.AllDay = newEvent.AllDay;
            app.Uri = !string.IsNullOrEmpty(newEvent.Url) ? new Uri(newEvent.Url) : null;

            if (newEvent.RecurrancePattern != null)
            {
                app.Recurrence = newEvent.RecurrancePattern.ConvertRule();
            }

            var cal = await instance.GetAppointmentCalendarAsync(newEvent.CalendarId);
            await cal.SaveAppointmentAsync(app);

            if (!string.IsNullOrEmpty(app.LocalId))
                return app.LocalId;

            throw new ArgumentException("[UWP]: Could not create appointment with supplied parameters");
        }

        static async Task<bool> PlatformUpdateCalendarEvent(CalendarEvent eventToUpdate)
        {
            await Permissions.RequestAsync<Permissions.CalendarWrite>();

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

            if (eventToUpdate.RecurrancePattern != null)
            {
                thisEvent.Recurrence = eventToUpdate.RecurrancePattern.ConvertRule();
            }

            var url = eventToUpdate.Url;
            if (!string.IsNullOrWhiteSpace(url))
            {
                url = eventToUpdate.Url;
                if (!Regex.IsMatch(url, @"^https?:\/\/", RegexOptions.IgnoreCase))
                {
                    url = "http://" + url;
                }
            }

            thisEvent.Subject = eventToUpdate.Title;
            thisEvent.Details = eventToUpdate.Description;
            thisEvent.Location = eventToUpdate.Location;
            thisEvent.StartTime = eventToUpdate.StartDate;
            thisEvent.Duration = eventToUpdate.EndDate.HasValue ? eventToUpdate.EndDate.Value - eventToUpdate.StartDate : TimeSpan.FromDays(1);
            thisEvent.AllDay = eventToUpdate.AllDay;
            thisEvent.Uri = !string.IsNullOrEmpty(url) ? new Uri(url) : null;

            var cal = await instance.GetAppointmentCalendarAsync(eventToUpdate.CalendarId);
            await cal.SaveAppointmentAsync(thisEvent);

            if (!string.IsNullOrEmpty(thisEvent.LocalId))
            {
                return true;
            }
            throw new ArgumentException("[UWP]: Could not update appointment with supplied parameters");
        }

        static AppointmentRecurrence ConvertRule(this RecurrenceRule recurrenceRule)
        {
            var eventRecurrence = new AppointmentRecurrence();
            eventRecurrence.Unit = (AppointmentRecurrenceUnit)recurrenceRule.Frequency;
            eventRecurrence.Interval = recurrenceRule.Interval;
            eventRecurrence.Until = recurrenceRule.EndDate;
            eventRecurrence.Occurrences = recurrenceRule.TotalOccurrences;

            switch (recurrenceRule.Frequency)
            {
                case RecurrenceFrequency.Daily:
                case RecurrenceFrequency.Weekly:
                    if (recurrenceRule.DaysOfTheWeek != null && recurrenceRule.DaysOfTheWeek.Count > 0)
                    {
                        eventRecurrence.DaysOfWeek = recurrenceRule.DaysOfTheWeek != null && recurrenceRule.DaysOfTheWeek.Count > 0 ? (AppointmentDaysOfWeek)ConvertIntListToBitFlag(recurrenceRule.DaysOfTheWeek.ConvertAll(delegate(DayOfTheWeek x) { return (int)x; })) : 0;
                        eventRecurrence.Unit = AppointmentRecurrenceUnit.Weekly;
                    }
                    break;
                case RecurrenceFrequency.Monthly:
                case RecurrenceFrequency.MonthlyOnDay:
                    if (recurrenceRule.DaysOfTheWeek != null && recurrenceRule.DaysOfTheWeek.Count > 0)
                    {
                        eventRecurrence.DaysOfWeek = (AppointmentDaysOfWeek)ConvertIntListToBitFlag(recurrenceRule.DaysOfTheWeek.ConvertAll(delegate(DayOfTheWeek x) { return (int)x; }));
                        eventRecurrence.WeekOfMonth = (AppointmentWeekOfMonth)recurrenceRule.WeekOfMonth;
                        eventRecurrence.Unit = AppointmentRecurrenceUnit.MonthlyOnDay;
                    }
                    else
                    {
                        eventRecurrence.Day = (uint)recurrenceRule.DayOfTheMonth;
                    }
                    break;
                case RecurrenceFrequency.Yearly:
                case RecurrenceFrequency.YearlyOnDay:
                    if (recurrenceRule.DaysOfTheWeek != null && recurrenceRule.DaysOfTheWeek.Count > 0)
                    {
                        eventRecurrence.WeekOfMonth = (AppointmentWeekOfMonth)recurrenceRule.WeekOfMonth;
                        eventRecurrence.DaysOfWeek = (AppointmentDaysOfWeek)ConvertIntListToBitFlag(recurrenceRule.DaysOfTheWeek.ConvertAll(delegate(DayOfTheWeek x) { return (int)x; }));
                        eventRecurrence.Unit = AppointmentRecurrenceUnit.YearlyOnDay;
                    }
                    else
                    {
                        eventRecurrence.Day = (uint)recurrenceRule.DayOfTheMonth;
                    }
                    eventRecurrence.Month = (uint)recurrenceRule.MonthOfTheYear;
                    break;
            }
            return eventRecurrence;
        }

        static async Task<string> PlatformCreateCalendar(Calendar newCalendar)
        {
            await Permissions.RequestAsync<Permissions.CalendarWrite>();

            var instance = await CalendarRequest.GetInstanceAsync();

            var cal = await instance.CreateAppointmentCalendarAsync(newCalendar.Name);

            if (cal != null)
                return cal.LocalId;

            throw new ArgumentException("[UWP]: Could not create appointment with supplied parameters");
        }

        static async Task<bool> PlatformDeleteCalendarEventInstanceByDate(string eventId, string calendarId, DateTimeOffset dateOfInstanceUtc)
        {
            await Permissions.RequestAsync<Permissions.CalendarWrite>();

            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentException("[UWP]: You must supply an event id to delete an event.");
            }
            var calendarEvent = await GetEventInstanceByIdAsync(eventId, dateOfInstanceUtc);

            if (calendarEvent.CalendarId != calendarId)
            {
                throw new ArgumentOutOfRangeException("[UWP]: Supplied event does not belong to supplied calendar");
            }

            var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
            var rect = new Windows.Foundation.Rect(0, 0, mainDisplayInfo.Width, mainDisplayInfo.Height);

            if (await AppointmentManager.ShowRemoveAppointmentAsync(calendarEvent.Id, rect, Windows.UI.Popups.Placement.Default, calendarEvent.StartDate))
            {
                return true;
            }
            return false;
        }

        static async Task<bool> PlatformDeleteCalendarEventById(string eventId, string calendarId)
        {
            await Permissions.RequestAsync<Permissions.CalendarWrite>();

            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentException("[UWP]: You must supply an event id to delete an event.");
            }
            var calendarEvent = await GetEventByIdAsync(eventId);

            if (calendarEvent.CalendarId != calendarId)
            {
                throw new ArgumentOutOfRangeException("[UWP]: Supplied event does not belong to supplied calendar");
            }

            var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
            var rect = new Windows.Foundation.Rect(0, 0, mainDisplayInfo.Width, mainDisplayInfo.Height);

            if (await AppointmentManager.ShowRemoveAppointmentAsync(eventId, rect, Windows.UI.Popups.Placement.Default))
            {
                return true;
            }
            return false;
        }

        static async Task<bool> PlatformAddAttendeeToEvent(CalendarEventAttendee newAttendee, string eventId)
        {
            await Permissions.RequestAsync<Permissions.CalendarWrite>();

            var instance = await CalendarRequest.GetInstanceAsync();

            var calendarEvent = await instance.GetAppointmentAsync(eventId);
            var cal = await instance.GetAppointmentCalendarAsync(calendarEvent.CalendarId);
            var cntInvitiees = calendarEvent.Invitees.Count;

            if (calendarEvent == null)
                throw new ArgumentException("[UWP]: You must supply a valid event id to add an attendee to.");

            calendarEvent.Invitees.Add(new AppointmentInvitee() { DisplayName = newAttendee.Name, Address = newAttendee.Email, Role = (AppointmentParticipantRole)(newAttendee.Type - 1) });
            await cal.SaveAppointmentAsync(calendarEvent);

            return calendarEvent.Invitees.Count == cntInvitiees + 1;
        }

        static async Task<bool> PlatformRemoveAttendeeFromEvent(CalendarEventAttendee newAttendee, string eventId)
        {
            await Permissions.RequestAsync<Permissions.CalendarWrite>();

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
