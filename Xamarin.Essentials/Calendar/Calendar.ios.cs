using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventKit;
using Foundation;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        static async Task<IEnumerable<DeviceCalendar>> PlatformGetCalendarsAsync()
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            EKCalendar[] calendars;
            try
            {
                calendars = CalendarRequest.Instance.Calendars;
            }
            catch (NullReferenceException ex)
            {
                throw new Exception($"iOS: Unexpected null reference exception {ex.Message}");
            }
            var calendarList = (from calendar in calendars
                                select new DeviceCalendar
                                {
                                    Id = calendar.CalendarIdentifier,
                                    Name = calendar.Title,
                                    IsReadOnly = !calendar.AllowsContentModifications
                                }).ToList();

            return calendarList;
        }

        static async Task<IEnumerable<DeviceEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var startDateToConvert = startDate ?? DateTimeOffset.Now.Add(defaultStartTimeFromNow);
            var endDateToConvert = endDate ?? startDateToConvert.Add(defaultEndTimeFromStartTime);  // NOTE: 4 years is the maximum period that a iOS calendar events can search
            var sDate = startDateToConvert.ToNSDate();
            var eDate = endDateToConvert.ToNSDate();
            EKCalendar[] calendars = null;
            if (!string.IsNullOrWhiteSpace(calendarId))
            {
                calendars = CalendarRequest.Instance.Calendars.Where(x => x.CalendarIdentifier == calendarId).ToArray();

                if (calendars.Length == 0 && !string.IsNullOrWhiteSpace(calendarId))
                    throw new ArgumentOutOfRangeException($"[iOS]: No calendar exists with the Id {calendarId}");
            }

            var query = CalendarRequest.Instance.PredicateForEvents(sDate, eDate, calendars);
            var events = CalendarRequest.Instance.EventsMatching(query);

            var eventList = (from e in events
                            select new DeviceEvent
                            {
                                Id = e.CalendarItemIdentifier,
                                CalendarId = e.Calendar.CalendarIdentifier,
                                Title = e.Title,
                                StartDate = e.StartDate.ToDateTime().AddHours(DateTimeOffset.Now.Offset.TotalHours),
                                EndDate = !e.AllDay ? (DateTimeOffset?)e.EndDate.ToDateTime().AddHours(DateTimeOffset.Now.Offset.TotalHours) : null
                            })
                            .OrderBy(e => e.StartDate)
                            .ToList();

            return eventList;
        }

        static async Task<DeviceEvent> PlatformGetEventByIdAsync(string eventId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            if (string.IsNullOrWhiteSpace(eventId))
            {
                throw new ArgumentException($"[iOS]: No Event found for event Id {eventId}");
            }

            var calendarEvent = CalendarRequest.Instance.GetCalendarItem(eventId) as EKEvent;
            if (calendarEvent == null)
            {
                throw new ArgumentOutOfRangeException($"[iOS]: No Event found for event Id {eventId}");
            }
            RecurrenceRule rule = null;
            if (calendarEvent.HasRecurrenceRules)
            {
                rule = new RecurrenceRule();
                var iOSRule = calendarEvent.RecurrenceRules[0];

                rule.Frequency = (RecurrenceFrequency)iOSRule.Frequency;
                rule.DaysOfTheWeek = iOSRule.DaysOfTheWeek != null ? iOSRule.DaysOfTheWeek.ToList().Select(x => (DayOfTheWeek)Convert.ToInt32(x.DayOfTheWeek)).ToList() : null;
                rule.Interval = (uint)iOSRule.Interval;

                if (iOSRule.DaysOfTheMonth != null && iOSRule.DaysOfTheMonth.Length > 0)
                    rule.DayOfTheMonth = (uint)iOSRule.DaysOfTheMonth?.First();

                if (iOSRule.MonthsOfTheYear != null && iOSRule.MonthsOfTheYear.Length > 0)
                    rule.MonthOfTheYear = (MonthOfTheYear)(uint)iOSRule.MonthsOfTheYear?.First();

                if (iOSRule.RecurrenceEnd?.EndDate != null)
                    rule.EndDate = iOSRule.RecurrenceEnd?.EndDate?.ToDateTime();

                if (iOSRule.RecurrenceEnd?.OccurrenceCount != null)
                    rule.TotalOccurrences = (uint?)iOSRule.RecurrenceEnd?.OccurrenceCount;

                // This will need an extension function as these don't line up
                // rule.Frequency = (RecurrenceFrequency)iOSRule.Frequency;
                // rule.DaysOfTheWeek = iOSRule.DaysOfTheWeek != null ? iOSRule.DaysOfTheWeek.ToList().Select(x => (DayOfTheWeek)Convert.ToInt32(x.DayOfTheWeek)).ToList() : null;
                // rule.Interval = (uint)iOSRule.Interval;
                // rule.StartOfTheWeek = (DayOfTheWeek)iOSRule.FirstDayOfTheWeek;
                // rule.WeeksOfTheYear = iOSRule.WeeksOfTheYear != null ? iOSRule.WeeksOfTheYear.Select(x => x.Int32Value).ToList() : null;
                // rule.DaysOfTheMonth = iOSRule.DaysOfTheMonth != null ? iOSRule.DaysOfTheMonth.Select(x => x.Int32Value).ToList() : null;
                // rule.DaysOfTheYear = iOSRule.DaysOfTheYear != null ? iOSRule.DaysOfTheYear.Select(x => x.Int32Value).ToList() : null;
                // rule.MonthsOfTheYear = iOSRule.MonthsOfTheYear != null ? iOSRule.MonthsOfTheYear.Select(x => (MonthOfTheYear)x.Int32Value).ToList() : null;
                // rule.EndDate = iOSRule.RecurrenceEnd?.EndDate?.ToDateTimeOffset();
                // rule.TotalOccurences = (uint?)iOSRule.RecurrenceEnd?.OccurrenceCount;

                // Might have to calculate occuerences based on frequency/days of year and so forth for iOS.
                // rule.TotalOccurences = (uint)iOSRule.??0
            }
            List<DeviceEventReminder> alarms = null;
            if (calendarEvent.HasAlarms)
            {
                alarms = new List<DeviceEventReminder>();
                foreach (var a in calendarEvent.Alarms)
                {
                    alarms.Add(new DeviceEventReminder() { MinutesPriorToEventStart = (calendarEvent.StartDate.ToDateTime() - a.AbsoluteDate.ToDateTime()).Minutes });
                }
            }
            var attendees = calendarEvent.Attendees != null ? GetAttendeesForEvent(calendarEvent.Attendees) : new List<DeviceEventAttendee>();
            if (calendarEvent.Organizer != null)
            {
                attendees.ToList().Insert(0, new DeviceEventAttendee
                {
                    Name = calendarEvent.Organizer.Name,
                    Email = calendarEvent.Organizer.Name,
                    Type = calendarEvent.Organizer.ParticipantRole.ToAttendeeType(),
                    IsOrganizer = true
                });
            }

            return new DeviceEvent
            {
                Id = calendarEvent.CalendarItemIdentifier,
                CalendarId = calendarEvent.Calendar.CalendarIdentifier,
                Title = calendarEvent.Title,
                Description = calendarEvent.Notes,
                Location = calendarEvent.Location,
                Url = calendarEvent.Url != null ? calendarEvent.Url.ToString() : string.Empty,
                StartDate = calendarEvent.StartDate.ToDateTime(),
                EndDate = !calendarEvent.AllDay ? (DateTimeOffset?)calendarEvent.EndDate.ToDateTime() : null,
                Attendees = attendees,
                RecurrancePattern = rule,
                Reminders = alarms
            };
        }

        static IEnumerable<DeviceEventAttendee> GetAttendeesForEvent(IEnumerable<EKParticipant> inviteList)
        {
            var attendees = (from attendee in inviteList
                             select new DeviceEventAttendee
                             {
                                 Name = attendee.Name,
                                 Email = attendee.Name,
                                 Type = attendee.ParticipantRole.ToAttendeeType()
                             })
                            .OrderBy(e => e.Name)
                            .ToList();

            return attendees;
        }

        static AttendeeType ToAttendeeType(this EKParticipantRole role)
        {
            switch (role)
            {
                case EKParticipantRole.Required:
                    return AttendeeType.Required;
                case EKParticipantRole.Optional:
                    return AttendeeType.Optional;
                case EKParticipantRole.NonParticipant:
                case EKParticipantRole.Chair:
                    return AttendeeType.Resource;
                case EKParticipantRole.Unknown:
                default:
                    return AttendeeType.None;
            }
        }

        static async Task<string> PlatformCreateCalendarEvent(DeviceEvent newEvent)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            if (string.IsNullOrEmpty(newEvent.CalendarId))
            {
                return string.Empty;
            }

            var evnt = EKEvent.FromStore(CalendarRequest.Instance);
            evnt = SetUpEvent(evnt, newEvent);

            if (CalendarRequest.Instance.SaveEvent(evnt, EKSpan.ThisEvent, true, out var error))
            {
                return evnt.EventIdentifier;
            }
            throw new ArgumentException("[iOS]: Could not create appointment with supplied parameters");
        }

        static async Task<bool> PlatformUpdateCalendarEvent(DeviceEvent eventToUpdate)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var existingEvent = await GetEventByIdAsync(eventToUpdate.Id);
            EKEvent thisEvent;
            if (string.IsNullOrEmpty(eventToUpdate.CalendarId) || existingEvent == null)
            {
                return false;
            }
            else if (existingEvent.CalendarId != eventToUpdate.CalendarId)
            {
                await DeleteCalendarEventById(existingEvent.Id, existingEvent.CalendarId);
                thisEvent = EKEvent.FromStore(CalendarRequest.Instance);
            }
            else
            {
                thisEvent = CalendarRequest.Instance.GetCalendarItem(eventToUpdate.Id) as EKEvent;
            }

            thisEvent = SetUpEvent(thisEvent, eventToUpdate);

            if (CalendarRequest.Instance.SaveEvent(thisEvent, EKSpan.ThisEvent, true, out var error))
            {
                return true;
            }
            throw new ArgumentException("[iOS]: Could not update appointment with supplied parameters");
        }

        static EKEvent SetUpEvent(EKEvent eventToUpdate, DeviceEvent eventToUpdateFrom)
        {
            eventToUpdate.Title = eventToUpdateFrom.Title;
            eventToUpdate.Calendar = CalendarRequest.Instance.GetCalendar(eventToUpdateFrom.CalendarId);
            eventToUpdate.Notes = eventToUpdateFrom.Description;
            eventToUpdate.Location = eventToUpdateFrom.Location;
            eventToUpdate.AllDay = eventToUpdateFrom.AllDay;
            eventToUpdate.StartDate = eventToUpdateFrom.StartDate.ToNSDate();
            eventToUpdate.Url = !string.IsNullOrWhiteSpace(eventToUpdateFrom.Url) ? new NSUrl(eventToUpdateFrom.Url) : null;
            eventToUpdate.EndDate = eventToUpdateFrom.EndDate.HasValue ? eventToUpdateFrom.EndDate.Value.ToNSDate() : eventToUpdateFrom.StartDate.AddDays(1).ToNSDate();
            if (eventToUpdateFrom.RecurrancePattern.Frequency != RecurrenceFrequency.None)
            {
                eventToUpdate.RecurrenceRules = new EKRecurrenceRule[1] { eventToUpdateFrom.RecurrancePattern.ConvertRule() };
            }
            return eventToUpdate;
        }

        static EKRecurrenceFrequency ConvertToiOS(this RecurrenceFrequency recurrenceFrequency)
        {
            switch (recurrenceFrequency)
            {
                case RecurrenceFrequency.Daily:
                    return EKRecurrenceFrequency.Daily;
                case RecurrenceFrequency.Weekly:
                    return EKRecurrenceFrequency.Weekly;
                case RecurrenceFrequency.Monthly:
                case RecurrenceFrequency.MonthlyOnDay:
                    return EKRecurrenceFrequency.Monthly;
                case RecurrenceFrequency.Yearly:
                case RecurrenceFrequency.YearlyOnDay:
                    return EKRecurrenceFrequency.Yearly;
                default:
                    return EKRecurrenceFrequency.Daily;
            }
        }

        static EKRecurrenceDayOfWeek[] ConvertToiOS(this List<DayOfTheWeek> daysOfTheWeek)
        {
            if (daysOfTheWeek == null || daysOfTheWeek.Count == 0)
                return null;

            var toReturn = new List<EKRecurrenceDayOfWeek>();
            foreach (var d in daysOfTheWeek)
            {
                toReturn.Add(EKRecurrenceDayOfWeek.FromDay(d.ConvertToiOS()));
            }
            return toReturn.ToArray();
        }

        static NSNumber[] ConvertToiOS(this uint dayOfTheMonth) => new NSNumber[1] { dayOfTheMonth };

        static EKDay ConvertToiOS(this DayOfTheWeek day) => (EKDay)day;

        static EKRecurrenceRule ConvertRule(this RecurrenceRule recurrenceRule) => new EKRecurrenceRule(
                type: recurrenceRule.Frequency.ConvertToiOS(),
                interval: (nint)recurrenceRule.Interval,
                days: recurrenceRule.Frequency != RecurrenceFrequency.Daily ? recurrenceRule.DaysOfTheWeek.ConvertToiOS() : null,
                monthDays: (recurrenceRule.DaysOfTheWeek != null && recurrenceRule.DaysOfTheWeek.Count > 0) ? null : recurrenceRule.DayOfTheMonth.ConvertToiOS(),
                months: recurrenceRule.Frequency == RecurrenceFrequency.Yearly ? ((uint)recurrenceRule.MonthOfTheYear).ConvertToiOS() : null,
                weeksOfTheYear: null,
                daysOfTheYear: null,
                setPositions: recurrenceRule.Frequency == RecurrenceFrequency.Yearly || recurrenceRule.Frequency == RecurrenceFrequency.Monthly ? ((uint)recurrenceRule.DayIterationOffSetPosition).ConvertToiOS() : null,
                end: recurrenceRule.EndDate.HasValue ? EKRecurrenceEnd.FromEndDate(recurrenceRule.EndDate.Value.ToNSDate()) : recurrenceRule.TotalOccurrences.HasValue ? EKRecurrenceEnd.FromOccurrenceCount((nint)recurrenceRule.TotalOccurrences.Value) : null);

        static async Task<bool> PlatformDeleteCalendarEventById(string eventId, string calendarId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentException("[iOS]: You must supply an event id to delete an event.");
            }

            var calendarEvent = CalendarRequest.Instance.GetCalendarItem(eventId) as EKEvent;

            if (calendarEvent.Calendar.CalendarIdentifier != calendarId)
            {
                throw new ArgumentOutOfRangeException("[iOS]: Supplied event does not belong to supplied calendar.");
            }

            if (CalendarRequest.Instance.RemoveEvent(calendarEvent, EKSpan.ThisEvent, true, out var error))
            {
                return true;
            }
            throw new Exception(error.DebugDescription);
        }

        static async Task<string> PlatformCreateCalendar(DeviceCalendar newCalendar)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var calendar = EKCalendar.Create(EKEntityType.Event, CalendarRequest.Instance);
            calendar.Title = newCalendar.Name;
            var source = CalendarRequest.Instance.Sources.Where(x => x.SourceType == EKSourceType.Local).FirstOrDefault();
            calendar.Source = source;

            if (CalendarRequest.Instance.SaveCalendar(calendar, true, out var error))
            {
                return calendar.CalendarIdentifier;
            }
            throw new Exception(error.DebugDescription);
        }

        // Not possible at this point in time from what I've found - https://stackoverflow.com/questions/28826222/add-invitees-to-calendar-event-programmatically-ios
        static async Task<bool> PlatformAddAttendeeToEvent(DeviceEventAttendee newAttendee, string eventId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var calendarEvent = CalendarRequest.Instance.GetCalendarItem(eventId) as EKEvent;

            var iOSAttendee = new NSObject();
            iOSAttendee.Init();
            iOSAttendee.SetValueForKey(NSObject.FromObject(newAttendee.Email), NSString.FromData(NSData.FromString("emailAddress", NSStringEncoding.Unicode), NSStringEncoding.Unicode));

            calendarEvent.Attendees.Append(iOSAttendee as EKParticipant);
            return true;
        }

        static async Task<bool> PlatformRemoveAttendeeFromEvent(DeviceEventAttendee newAttendee, string eventId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var calendarEvent = CalendarRequest.Instance.GetCalendarItem(eventId) as EKEvent;

            var calendarEventAttendees = calendarEvent.Attendees.ToList();
            calendarEventAttendees.RemoveAll(x => x.Name == newAttendee.Name);

            // calendarEvent.Attendees = calendarEventAttendees; - readonly cannot be done at this stage.

            if (CalendarRequest.Instance.SaveEvent(calendarEvent, EKSpan.ThisEvent, true, out var error))
            {
                return true;
            }
            throw new Exception(error.DebugDescription);
        }
    }
}
