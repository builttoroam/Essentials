using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Provider;

namespace Xamarin.Essentials
{
    public static partial class Calendar
    {
        const string andCondition = "AND";
        const string dailyFrequency = "DAILY";
        const string weeklyFrequency = "WEEKLY";
        const string monthlyFrequency = "MONTHLY";
        const string yearlyFrequency = "YEARLY";
        const string ruleFrequency = "FREQ=";
        const string ruleCount = "COUNT=";
        const string ruleInterval = "INTERVAL=";
        const string ruleEnd = "UNTIL=";
        const string ruleByDay = "BYDAY=";
        const string ruleDevider = ";";

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
            cur.Dispose();
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

        static async Task<IReadOnlyList<IEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
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
            };
            var calendarSpecificEvent = string.Empty;
            if (!string.IsNullOrEmpty(calendarId))
            {
                calendarSpecificEvent = $"{CalendarContract.Events.InterfaceConsts.CalendarId}={calendarId} {andCondition} ";
            }
            if (startDate != null)
            {
                calendarSpecificEvent += $"{CalendarContract.Events.InterfaceConsts.Dtstart} >= {startDate.Value.ToUnixTimeMilliseconds()} {andCondition} ";
            }
            if (endDate != null)
            {
                calendarSpecificEvent += $"{CalendarContract.Events.InterfaceConsts.Dtend} <= {endDate.Value.ToUnixTimeMilliseconds()} {andCondition} ";
            }
            if (calendarSpecificEvent != string.Empty)
            {
                calendarSpecificEvent = calendarSpecificEvent.Substring(0, calendarSpecificEvent.LastIndexOf($" {andCondition} ", StringComparison.Ordinal));
            }

            var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(eventsUri, eventsProjection.ToArray(), calendarSpecificEvent, null, $"{CalendarContract.Events.InterfaceConsts.Dtstart} ASC");
            var events = new List<IEvent>();
            while (cur.MoveToNext())
            {
                events.Add(new Event()
                {
                    Id = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Id)),
                    CalendarId = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.CalendarId)),
                    Title = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Title)),
                    Start = cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtstart)),
                    End = cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtend)),
                });
            }
            cur.Dispose();
            return events.AsReadOnly();
        }

        static async Task<IEvent> PlatformGetEventByIdAsync(string eventId)
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
            var calendarSpecificEvent = $"{CalendarContract.Events.InterfaceConsts.Id}={eventId}";
            var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(eventsUri, eventsProjection.ToArray(), calendarSpecificEvent, null, null);

            try
            {
                var rRule = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Rrule));
                var eventResult = new Event
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
                    Attendees = GetAttendeesForEvent(eventId),
                    RecurrancePattern = !string.IsNullOrEmpty(rRule) ? GetRecurranceRuleForEvent(rRule) : null
                };
                cur.Dispose();
                return eventResult;
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException($"[Android]: No Event found for event Id {eventId}");
            }
        }

        static IReadOnlyList<IAttendee> GetAttendeesForEvent(string eventId)
        {
            var attendeesUri = CalendarContract.Attendees.ContentUri;
            var attendeesProjection = new List<string>
            {
                CalendarContract.Attendees.InterfaceConsts.EventId,
                CalendarContract.Attendees.InterfaceConsts.AttendeeEmail,
                CalendarContract.Attendees.InterfaceConsts.AttendeeName
            };
            var attendeeSpecificAttendees = $"{CalendarContract.Attendees.InterfaceConsts.EventId}={eventId}";
            var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(attendeesUri, attendeesProjection.ToArray(), attendeeSpecificAttendees, null, null);
            var attendees = new List<IAttendee>();
            while (cur.MoveToNext())
            {
                attendees.Add(new Attendee()
                {
                    Name = cur.GetString(attendeesProjection.IndexOf(CalendarContract.Attendees.InterfaceConsts.AttendeeName)),
                    Email = cur.GetString(attendeesProjection.IndexOf(CalendarContract.Attendees.InterfaceConsts.AttendeeEmail)),
                });
            }
            cur.Dispose();
            return attendees.AsReadOnly();
        }

        static RecurrenceRule GetRecurranceRuleForEvent(string rule)
        {
            var recurranceRule = new RecurrenceRule();
            if (rule.Contains(ruleFrequency))
            {
                var ruleFrequencyResult = rule.Substring(rule.IndexOf(ruleFrequency, StringComparison.Ordinal) + ruleFrequency.Length);
                ruleFrequencyResult = ruleFrequencyResult.Contains(ruleDevider) ? ruleFrequencyResult.Substring(0, ruleFrequencyResult.IndexOf(ruleDevider)) : ruleFrequencyResult;
                switch (ruleFrequencyResult)
                {
                    case dailyFrequency:
                        recurranceRule.Frequency = RecurrenceFrequency.Daily;
                        break;
                    case weeklyFrequency:
                        recurranceRule.Frequency = RecurrenceFrequency.Weekly;
                        break;
                    case monthlyFrequency:
                        recurranceRule.Frequency = RecurrenceFrequency.Monthly;
                        break;
                    case yearlyFrequency:
                        recurranceRule.Frequency = RecurrenceFrequency.Yearly;
                        break;
                }
            }

            if (rule.Contains(ruleInterval))
            {
                var ruleIntervalResult = rule.Substring(rule.IndexOf(ruleInterval, StringComparison.Ordinal) + ruleInterval.Length);
                ruleIntervalResult = ruleIntervalResult.Contains(ruleDevider) ? ruleIntervalResult.Substring(0, ruleIntervalResult.IndexOf(ruleDevider, StringComparison.Ordinal)) : ruleIntervalResult;
                recurranceRule.Interval = int.Parse(ruleIntervalResult);
            }

            if (rule.Contains(ruleCount))
            {
                var ruleCountResult = rule.Substring(rule.IndexOf(ruleCount, StringComparison.Ordinal) + ruleCount.Length);
                ruleCountResult = ruleCountResult.Contains(ruleDevider) ? ruleCountResult.Substring(0, ruleCountResult.IndexOf(ruleDevider, StringComparison.Ordinal)) : ruleCountResult;
                recurranceRule.TotalOccurences = int.Parse(ruleCountResult);
            }

            if (rule.Contains(ruleEnd))
            {
                var ruleEndDate = rule.Substring(rule.IndexOf(ruleEnd, StringComparison.Ordinal) + ruleEnd.Length);
                ruleEndDate = ruleEndDate.Contains(ruleDevider) ? ruleEndDate.Substring(0, ruleEndDate.IndexOf(ruleDevider, StringComparison.Ordinal)) : ruleEndDate;
                recurranceRule.EndDate = DateTime.Parse(ruleEndDate).ToLocalTime();
            }

            if (rule.Contains(ruleByDay))
            {
                var ruleOccurenceDays = rule.Substring(rule.IndexOf(ruleByDay, StringComparison.Ordinal) + ruleByDay.Length);
                ruleOccurenceDays = ruleOccurenceDays.Contains(ruleDevider) ? ruleOccurenceDays.Substring(0, ruleOccurenceDays.IndexOf(ruleDevider, StringComparison.Ordinal)) : ruleOccurenceDays;
                recurranceRule.DaysOfTheWeek = new List<DayOfTheWeek>();
                foreach (var d in ruleOccurenceDays.Split(','))
                {
                    switch (d)
                    {
                        case "MO":
                            recurranceRule.DaysOfTheWeek.Add(DayOfTheWeek.Monday);
                            break;
                        case "TU":
                            recurranceRule.DaysOfTheWeek.Add(DayOfTheWeek.Tuesday);
                            break;
                        case "WE":
                            recurranceRule.DaysOfTheWeek.Add(DayOfTheWeek.Wednesday);
                            break;
                        case "TH":
                            recurranceRule.DaysOfTheWeek.Add(DayOfTheWeek.Thursday);
                            break;
                        case "FR":
                            recurranceRule.DaysOfTheWeek.Add(DayOfTheWeek.Friday);
                            break;
                        case "SA":
                            recurranceRule.DaysOfTheWeek.Add(DayOfTheWeek.Saturday);
                            break;
                        case "SU":
                            recurranceRule.DaysOfTheWeek.Add(DayOfTheWeek.Sunday);
                            break;
                    }
                }
            }
            return recurranceRule;
        }
    }
}
