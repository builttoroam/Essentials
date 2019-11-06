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

            cur.MoveToNext();
            if (cur.IsFirst && cur.IsLast)
            {
                var rRule = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Rrule));
                return new Event
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
            }
            else
            {
                throw new Exception("o oh");
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
            return attendees.AsReadOnly();
        }

        static RecurrenceRule GetRecurranceRuleForEvent(string rule)
        {
            var recurranceRule = new RecurrenceRule();
            if (rule.Contains("FREQ="))
            {
                var ruleFrequency = rule.Substring(rule.IndexOf("FREQ=", StringComparison.Ordinal) + 5);
                ruleFrequency = ruleFrequency.Substring(0, ruleFrequency.IndexOf(";"));
                switch (ruleFrequency)
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

            if (rule.Contains("INTERVAL="))
            {
                var ruleInterval = rule.Substring(rule.IndexOf("INTERVAL=", StringComparison.Ordinal) + 9);
                ruleInterval = ruleInterval.Substring(0, ruleInterval.IndexOf(";", StringComparison.Ordinal));
                recurranceRule.Interval = int.Parse(ruleInterval);
            }

            if (rule.Contains("COUNT="))
            {
                var ruleOccurences = rule.Substring(rule.IndexOf("COUNT=", StringComparison.Ordinal) + 6);
                ruleOccurences = ruleOccurences.Substring(0, ruleOccurences.IndexOf(";", StringComparison.Ordinal));
                recurranceRule.TotalOccurences = int.Parse(ruleOccurences);
            }

            if (rule.Contains("UNTIL="))
            {
                var ruleEndDate = rule.Substring(rule.IndexOf("UNTIL=", StringComparison.Ordinal) + 6);
                ruleEndDate = ruleEndDate.Substring(0, ruleEndDate.IndexOf(";", StringComparison.Ordinal));
                recurranceRule.EndDate = DateTime.Parse(ruleEndDate).ToLocalTime();
            }

            if (rule.Contains("BYDAY="))
            {
                var ruleOccurenceDays = rule.Substring(rule.IndexOf("BYDAY=", StringComparison.Ordinal) + 6);
                ruleOccurenceDays = ruleOccurenceDays.Contains(";") ? ruleOccurenceDays.Substring(0, ruleOccurenceDays.IndexOf(";", StringComparison.Ordinal)) : ruleOccurenceDays;
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
