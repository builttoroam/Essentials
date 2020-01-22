using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventKit;
using Foundation;

namespace Xamarin.Essentials
{
    public static partial class Calendars
    {
        static async Task<IEnumerable<Calendar>> PlatformGetCalendarsAsync()
        {
            await Permissions.RequestAsync<Permissions.CalendarRead>();

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
                                select new Calendar
                                {
                                    Id = calendar.CalendarIdentifier,
                                    Name = calendar.Title,
                                    IsReadOnly = !calendar.AllowsContentModifications
                                }).ToList();

            return calendarList;
        }

        static async Task<IEnumerable<CalendarEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            await Permissions.RequestAsync<Permissions.CalendarRead>();

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
                            select new CalendarEvent
                            {
                                Id = e.CalendarItemIdentifier,
                                CalendarId = e.Calendar.CalendarIdentifier,
                                Title = e.Title,
                                StartDate = e.StartDate.ToDateTimeOffsetWithTimeZone(e.TimeZone),
                                EndDate = !e.AllDay ? (DateTimeOffset?)e.EndDate.ToDateTimeOffsetWithTimeZone(e.TimeZone) : null
                            })
                            .OrderBy(e => e.StartDate)
                            .ToList();

            return eventList;
        }


        static DateTimeOffset ToDateTimeOffsetWithTimeZone(this NSDate originalDate, NSTimeZone timeZone)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone != null ? timeZone.Name : NSTimeZone.LocalTimeZone.Name);
            return TimeZoneInfo.ConvertTime(originalDate.ToDateTime(), tz);
        }

        static async Task<CalendarEvent> PlatformGetEventByIdAsync(string eventId)
        {
            await Permissions.RequestAsync<Permissions.CalendarRead>();

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
                rule = GetRecurrenceRule(calendarEvent.RecurrenceRules[0], calendarEvent.TimeZone);
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

            return new CalendarEvent
            {
                Id = calendarEvent.CalendarItemIdentifier,
                CalendarId = calendarEvent.Calendar.CalendarIdentifier,
                Title = calendarEvent.Title,
                Description = calendarEvent.Notes,
                Location = calendarEvent.Location,
                Url = calendarEvent.Url != null ? calendarEvent.Url.ToString() : string.Empty,
                StartDate = calendarEvent.StartDate.ToDateTimeOffsetWithTimeZone(calendarEvent.TimeZone),
                EndDate = !calendarEvent.AllDay ? (DateTimeOffset?)calendarEvent.EndDate.ToDateTimeOffsetWithTimeZone(calendarEvent.TimeZone) : null,
                Attendees = attendees,
                RecurrancePattern = rule,
                Reminders = alarms
            };
        }

        static async Task<CalendarEvent> PlatformGetEventInstanceByIdAsync(string eventId, DateTimeOffset instanceDate)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            if (string.IsNullOrWhiteSpace(eventId))
            {
                throw new ArgumentException($"[iOS]: No Event found for event Id {eventId}");
            }

            var calendarEvent = CalendarRequest.Instance.GetCalendarItem(eventId) as EKEvent;
            var instanceOfEvent = (await GetEventsAsync(calendarEvent.Calendar.CalendarIdentifier, instanceDate, instanceDate.AddDays(1))).FirstOrDefault(x => x.Id == eventId);

            calendarEvent.StartDate = instanceOfEvent.StartDate.ToNSDate();
            calendarEvent.EndDate = instanceOfEvent.AllDay ? null : instanceOfEvent.EndDate.Value.ToNSDate();
            if (calendarEvent == null)
            {
                throw new ArgumentOutOfRangeException($"[iOS]: No Event found for event Id {eventId}");
            }

            RecurrenceRule rule = null;
            if (calendarEvent.HasRecurrenceRules)
            {
                rule = GetRecurrenceRule(calendarEvent.RecurrenceRules[0], calendarEvent.TimeZone);
            }
            List<DeviceEventReminder> alarms = null;
            if (calendarEvent.HasAlarms)
            {
                alarms = new List<DeviceEventReminder>();
                foreach (var a in calendarEvent.Alarms)
                {
                    alarms.Add(new DeviceEventReminder() { MinutesPriorToEventStart = (calendarEvent.StartDate.ToDateTimeOffsetWithTimeZone(calendarEvent.TimeZone) - a.AbsoluteDate.ToDateTimeOffsetWithTimeZone(calendarEvent.TimeZone)).Minutes });
                }
            }
            var attendees = calendarEvent.Attendees != null ? GetAttendeesForEvent(calendarEvent.Attendees) : new List<CalendarEventAttendee>();
            if (calendarEvent.Organizer != null)
            {
                attendees.ToList().Insert(0, new CalendarEventAttendee
                {
                    Name = calendarEvent.Organizer.Name,
                    Email = calendarEvent.Organizer.Name,
                    Type = calendarEvent.Organizer.ParticipantRole.ToAttendeeType(),
                    IsOrganizer = true
                });
            }
            return new CalendarEvent
            {
                Id = calendarEvent.CalendarItemIdentifier,
                CalendarId = calendarEvent.Calendar.CalendarIdentifier,
                Title = calendarEvent.Title,
                Description = calendarEvent.Notes,
                Location = calendarEvent.Location,
                Url = calendarEvent.Url != null ? calendarEvent.Url.ToString() : string.Empty,
                StartDate = calendarEvent.StartDate.ToDateTimeOffsetWithTimeZone(calendarEvent.TimeZone),
                EndDate = !calendarEvent.AllDay ? (DateTimeOffset?)calendarEvent.EndDate.ToDateTimeOffsetWithTimeZone(calendarEvent.TimeZone) : null,
                Attendees = attendees,
                RecurrancePattern = rule,
                Reminders = alarms
            };
        }

        static RecurrenceRule GetRecurrenceRule(this EKRecurrenceRule iOSRule, NSTimeZone timeZone)
        {
            var rule = new RecurrenceRule();
            rule.Frequency = (RecurrenceFrequency)iOSRule.Frequency;
            if (iOSRule.DaysOfTheWeek != null)
            {
                rule = iOSRule.DaysOfTheWeek.ConvertToDayOfTheWeekList(rule);
            }
            rule.Interval = (uint)iOSRule.Interval;

            if (iOSRule.SetPositions != null)
            {
                if (iOSRule.SetPositions.Length > 0)
                {
                    var day = iOSRule.SetPositions[0] as NSNumber;
                    rule.WeekOfMonth = (IterationOffset)((int)day);
                    if (rule.Frequency == RecurrenceFrequency.Monthly)
                    {
                        rule.Frequency = RecurrenceFrequency.MonthlyOnDay;
                    }
                    else
                    {
                        rule.Frequency = RecurrenceFrequency.YearlyOnDay;
                    }
                }
            }

            if (iOSRule.DaysOfTheMonth != null)
            {
                if (iOSRule.DaysOfTheMonth.Count() > 0)
                {
                    rule.DayOfTheMonth = (uint)iOSRule.DaysOfTheMonth?.FirstOrDefault();
                }
            }

            if (iOSRule.MonthsOfTheYear != null)
            {
                if (iOSRule.MonthsOfTheYear.Count() > 0)
                {
                    rule.MonthOfTheYear = (MonthOfYear)(uint)iOSRule.MonthsOfTheYear?.FirstOrDefault();
                }
            }

            rule.EndDate = iOSRule.RecurrenceEnd?.EndDate?.ToDateTimeOffsetWithTimeZone(timeZone);

            rule.TotalOccurrences = (uint?)iOSRule.RecurrenceEnd?.OccurrenceCount;

            return rule;
        }

        static RecurrenceRule ConvertToDayOfTheWeekList(this EKRecurrenceDayOfWeek[] recurrenceDays, RecurrenceRule rule)
        {
            rule.DaysOfTheWeek = recurrenceDays.ToList().Select(x => (DayOfTheWeek)Convert.ToInt32(x.DayOfTheWeek)).ToList();

            foreach (var d in recurrenceDays)
            {
                if (d.WeekNumber != 0)
                {
                    if (rule.Frequency == RecurrenceFrequency.Monthly)
                    {
                        rule.Frequency = RecurrenceFrequency.MonthlyOnDay;
                    }
                    else
                    {
                        rule.Frequency = RecurrenceFrequency.YearlyOnDay;
                    }
                    rule.WeekOfMonth = (IterationOffset)(int)(d.WeekNumber - 1);
                }
            }
            return rule;
        }

        static IEnumerable<CalendarEventAttendee> GetAttendeesForEvent(IEnumerable<EKParticipant> inviteList)
        {
            var attendees = (from attendee in inviteList
                             select new CalendarEventAttendee
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

        static async Task<string> PlatformCreateCalendarEvent(CalendarEvent newEvent)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            if (string.IsNullOrEmpty(newEvent.CalendarId))
            {
                return string.Empty;
            }

            var evnt = EKEvent.FromStore(CalendarRequest.Instance);
            evnt = SetUpEvent(evnt, newEvent);
            var error = new NSError();

            if (CalendarRequest.Instance.SaveEvent(evnt, EKSpan.FutureEvents, true, out error))
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

            var error = new NSError();

            if (CalendarRequest.Instance.SaveEvent(thisEvent, EKSpan.FutureEvents, true, out error))
            {
                return true;
            }
            throw new ArgumentException("[iOS]: Could not update appointment with supplied parameters");
        }

        static EKEvent SetUpEvent(EKEvent eventToUpdate, DeviceEvent eventToUpdateFrom)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(NSTimeZone.LocalTimeZone.Name);
            eventToUpdate.Title = eventToUpdateFrom.Title;
            eventToUpdate.Calendar = CalendarRequest.Instance.GetCalendar(eventToUpdateFrom.CalendarId);
            eventToUpdate.Notes = eventToUpdateFrom.Description;
            eventToUpdate.Location = eventToUpdateFrom.Location;
            eventToUpdate.AllDay = eventToUpdateFrom.AllDay;
            eventToUpdate.StartDate = TimeZoneInfo.ConvertTime(eventToUpdateFrom.StartDate, tz).ToNSDate();
            eventToUpdate.TimeZone = NSTimeZone.LocalTimeZone;
            eventToUpdate.Url = !string.IsNullOrWhiteSpace(eventToUpdateFrom.Url) ? new NSUrl(eventToUpdateFrom.Url) : null;
            eventToUpdate.EndDate = eventToUpdateFrom.EndDate.HasValue ? TimeZoneInfo.ConvertTime(eventToUpdateFrom.EndDate.Value, tz).ToNSDate() : TimeZoneInfo.ConvertTime(eventToUpdateFrom.StartDate, tz).AddDays(1).ToNSDate();
            if (eventToUpdateFrom.RecurrancePattern != null && eventToUpdateFrom.RecurrancePattern.Frequency != null)
            {
                eventToUpdate.RecurrenceRules = new EKRecurrenceRule[1] { eventToUpdateFrom.RecurrancePattern.ConvertRule() };
            }
            return eventToUpdate;
        }

        static EKRecurrenceFrequency ConvertToiOS(this RecurrenceFrequency? recurrenceFrequency)
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

        static NSNumber[] ConvertToiOS(this int dayOfTheMonth) => new NSNumber[1] { dayOfTheMonth };

        static EKDay ConvertToiOS(this DayOfTheWeek day) => (EKDay)day;

        static EKRecurrenceRule ConvertRule(this RecurrenceRule recurrenceRule) => new EKRecurrenceRule(
                type: recurrenceRule.Frequency.ConvertToiOS(),
                interval: (nint)recurrenceRule.Interval,
                days: recurrenceRule.Frequency != RecurrenceFrequency.Daily ? recurrenceRule.DaysOfTheWeek.ConvertToiOS() : null,
                monthDays: (recurrenceRule.DaysOfTheWeek != null && recurrenceRule.DaysOfTheWeek.Count > 0) ? null : ((int)recurrenceRule.DayOfTheMonth).ConvertToiOS(),
                months: recurrenceRule.Frequency == RecurrenceFrequency.Yearly ? ((int)recurrenceRule.MonthOfTheYear).ConvertToiOS() : null,
                weeksOfTheYear: null,
                daysOfTheYear: null,
                setPositions: recurrenceRule.Frequency == RecurrenceFrequency.Yearly || recurrenceRule.Frequency == RecurrenceFrequency.Monthly ? ((int)recurrenceRule.WeekOfMonth).ConvertToiOS() : null,
                end: recurrenceRule.EndDate.HasValue ? EKRecurrenceEnd.FromEndDate(TimeZoneInfo.ConvertTime(recurrenceRule.EndDate.Value, TimeZoneInfo.Local).ToNSDate()) : recurrenceRule.TotalOccurrences.HasValue ? EKRecurrenceEnd.FromOccurrenceCount((nint)recurrenceRule.TotalOccurrences.Value) : null);

        static async Task<bool> PlatformDeleteCalendarEventInstanceByDate(string eventId, string calendarId, DateTimeOffset dateOfInstanceUtc)
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

            if (CalendarRequest.Instance.RemoveEvent(calendarEvent, EKSpan.FutureEvents, true, out var error))
            {
                return true;
            }
            throw new Exception(error.DebugDescription);
        }

        static async Task<string> PlatformCreateCalendar(Calendar newCalendar)
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
        static async Task<bool> PlatformAddAttendeeToEvent(CalendarEventAttendee newAttendee, string eventId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var attendee = ObjCRuntime.Class.GetHandle("EKAttendee");

            var tst = ObjCRuntime.Runtime.GetNSObject(attendee);
            var email = new NSString("emailAddress");

            // tst.Init();
            tst.SetValueForKey(new NSString(newAttendee.Email), email);

            var result = tst as EKParticipant;
            return true;
        }

        static async Task<bool> PlatformRemoveAttendeeFromEvent(CalendarEventAttendee newAttendee, string eventId)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

            var calendarEvent = CalendarRequest.Instance.GetCalendarItem(eventId) as EKEvent;

            var calendarEventAttendees = calendarEvent.Attendees.ToList();
            calendarEventAttendees.RemoveAll(x => x.Name == newAttendee.Name);

            // calendarEvent.Attendees = calendarEventAttendees; - readonly cannot be done at this stage.

            if (CalendarRequest.Instance.SaveEvent(calendarEvent, EKSpan.FutureEvents, true, out var error))
            {
                return true;
            }
            throw new Exception(error.DebugDescription);
        }
    }
}
