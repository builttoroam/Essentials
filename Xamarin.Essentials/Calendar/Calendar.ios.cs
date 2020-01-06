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
                                StartDate = e.StartDate.ToDateTimeOffset(),
                                EndDate = !e.AllDay ? (DateTimeOffset?)e.EndDate.ToDateTimeOffset() : null
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

                // This will need an extension function as these don't line up
                rule.Frequency = (RecurrenceFrequency)iOSRule.Frequency;
                rule.DaysOfTheWeek = iOSRule.DaysOfTheWeek != null ? iOSRule.DaysOfTheWeek.ToList().Select(x => (DayOfTheWeek)Convert.ToInt32(x.DayOfTheWeek)).ToList() : null;
                rule.Interval = (uint)iOSRule.Interval;
                rule.StartOfTheWeek = (DayOfTheWeek)iOSRule.FirstDayOfTheWeek;
                rule.WeeksOfTheYear = iOSRule.WeeksOfTheYear != null ? iOSRule.WeeksOfTheYear.Select(x => x.Int32Value).ToList() : null;
                rule.DaysOfTheMonth = iOSRule.DaysOfTheMonth != null ? iOSRule.DaysOfTheMonth.Select(x => x.Int32Value).ToList() : null;
                rule.DaysOfTheYear = iOSRule.DaysOfTheYear != null ? iOSRule.DaysOfTheYear.Select(x => x.Int32Value).ToList() : null;
                rule.MonthsOfTheYear = iOSRule.MonthsOfTheYear != null ? iOSRule.MonthsOfTheYear.Select(x => (MonthOfTheYear)x.Int32Value).ToList() : null;
                rule.EndDate = iOSRule.RecurrenceEnd?.EndDate?.ToDateTimeOffset();
                rule.TotalOccurences = (uint?)iOSRule.RecurrenceEnd?.OccurrenceCount;

                // Might have to calculate occuerences based on frequency/days of year and so forth for iOS.
                // rule.TotalOccurences = (uint)iOSRule.??0
            }
            List<DeviceEventReminder> alarms = null;
            if (calendarEvent.HasAlarms)
            {
                alarms = new List<DeviceEventReminder>();
                foreach (var a in calendarEvent.Alarms)
                {
                    alarms.Add(new DeviceEventReminder() { MinutesPriorToEventStart = (calendarEvent.StartDate.ToDateTimeOffset() - a.AbsoluteDate.ToDateTimeOffset()).Minutes });
                }
            }

            return new DeviceEvent
            {
                Id = calendarEvent.CalendarItemIdentifier,
                CalendarId = calendarEvent.Calendar.CalendarIdentifier,
                Title = calendarEvent.Title,
                Description = calendarEvent.Notes,
                Location = calendarEvent.Location,
                StartDate = calendarEvent.StartDate.ToDateTimeOffset(),
                EndDate = !calendarEvent.AllDay ? (DateTimeOffset?)calendarEvent.EndDate.ToDateTimeOffset() : null,
                Attendees = calendarEvent.Attendees != null ? GetAttendeesForEvent(calendarEvent.Attendees) : new List<DeviceEventAttendee>(),
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
                                 Required = attendee.ParticipantRole == EKParticipantRole.Required
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

            var evnt = EKEvent.FromStore(CalendarRequest.Instance);
            evnt.Title = newEvent.Title;
            evnt.Calendar = CalendarRequest.Instance.GetCalendar(newEvent.CalendarId);
            evnt.Notes = newEvent.Description;
            evnt.Location = newEvent.Location;
            evnt.AllDay = newEvent.AllDay;
            evnt.StartDate = newEvent.StartDate.ToNSDate();
            evnt.EndDate = newEvent.EndDate.HasValue ? newEvent.EndDate.Value.ToNSDate() : newEvent.StartDate.AddDays(1).ToNSDate();

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
            EKEvent thisEvent = null;

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

            thisEvent.Title = eventToUpdate.Title;
            thisEvent.Calendar = CalendarRequest.Instance.GetCalendar(eventToUpdate.CalendarId);
            thisEvent.Notes = eventToUpdate.Description;
            thisEvent.Location = eventToUpdate.Location;
            thisEvent.AllDay = eventToUpdate.AllDay;
            thisEvent.StartDate = eventToUpdate.StartDate.ToNSDate();
            thisEvent.EndDate = eventToUpdate.EndDate.HasValue ? eventToUpdate.EndDate.Value.ToNSDate() : eventToUpdate.StartDate.AddDays(1).ToNSDate();

            if (CalendarRequest.Instance.SaveEvent(thisEvent, EKSpan.ThisEvent, true, out var error))
            {
                return true;
            }
            throw new ArgumentException("[iOS]: Could not update appointment with supplied parameters");
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
