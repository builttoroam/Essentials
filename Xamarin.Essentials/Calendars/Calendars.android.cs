﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Android.Content;
using Android.Database;
using Android.Provider;
using Java.Security;

namespace Xamarin.Essentials
{
    public static partial class Calendars
    {
        const string andCondition = "AND";
        const string dailyFrequency = "DAILY";
        const string weeklyFrequency = "WEEKLY";
        const string monthlyFrequency = "MONTHLY";
        const string yearlyFrequency = "YEARLY";

        static async Task<IEnumerable<Calendar>> PlatformGetCalendarsAsync()
        {
            await Permissions.RequestAsync<Permissions.CalendarRead>();

            var calendarsUri = CalendarContract.Calendars.ContentUri;
            var calendarsProjection = new List<string>
            {
                CalendarContract.Calendars.InterfaceConsts.Id,
                CalendarContract.Calendars.InterfaceConsts.CalendarAccessLevel,
                CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName
            };
            var queryConditions = $"{CalendarContract.Calendars.InterfaceConsts.Deleted} != 1";

            using (var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(calendarsUri, calendarsProjection.ToArray(), queryConditions, null, null))
            {
                var calendars = new List<Calendar>();
                while (cur.MoveToNext())
                {
                    calendars.Add(new Calendar()
                    {
                        Id = cur.GetString(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.Id)),
                        IsReadOnly = IsCalendarReadOnly((CalendarAccess)cur.GetInt(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.CalendarAccessLevel))),
                        Name = cur.GetString(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName)),
                    });
                }
                return calendars;
            }
        }

        static bool IsCalendarReadOnly(CalendarAccess accessLevel)
        {
            switch (accessLevel)
            {
                case CalendarAccess.AccessContributor:
                case CalendarAccess.AccessRoot:
                case CalendarAccess.AccessOwner:
                case CalendarAccess.AccessEditor:
                    return true;
                default:
                    return false;
            }
        }
      
        static async Task<IEnumerable<CalendarEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            await Permissions.RequestAsync<Permissions.CalendarRead>();

            var sDate = startDate ?? DateTimeOffset.Now.Add(defaultStartTimeFromNow);
            var eDate = endDate ?? sDate.Add(defaultEndTimeFromStartTime);

            var eventsProjection = new List<string>
            {
                CalendarContract.Instances.EventId,
                CalendarContract.Instances.Begin,
                CalendarContract.Instances.End,
                CalendarContract.Events.InterfaceConsts.EventTimezone,
                CalendarContract.Events.InterfaceConsts.EventEndTimezone,
                CalendarContract.Events.InterfaceConsts.CalendarId,
                CalendarContract.Events.InterfaceConsts.Title
            };
            var instanceUriBuilder = CalendarContract.Instances.ContentUri.BuildUpon();
            ContentUris.AppendId(instanceUriBuilder, sDate.AddMilliseconds(sDate.Offset.TotalMilliseconds).ToUnixTimeMilliseconds());
            ContentUris.AppendId(instanceUriBuilder, eDate.AddMilliseconds(eDate.Offset.TotalMilliseconds).ToUnixTimeMilliseconds());

            var instancesUri = instanceUriBuilder.Build();
            var calendarSpecificEvent = string.Empty;
          
            if (!string.IsNullOrEmpty(calendarId))
            {
                // Android event ids are always integers
                if (!int.TryParse(calendarId, out var resultId))
                {
                    throw new ArgumentException($"[Android]: No Event found for event Id {calendarId}");
                }
                calendarSpecificEvent = $"{CalendarContract.Events.InterfaceConsts.CalendarId} = {resultId} {andCondition} ";
            }
            calendarSpecificEvent += $"{CalendarContract.Events.InterfaceConsts.Deleted} != 1";

            var instances = new List<CalendarEvent>();
            using (var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(instancesUri, eventsProjection.ToArray(), calendarSpecificEvent, null, $"{CalendarContract.Instances.Begin} ASC"))
            {
                while (cur.MoveToNext())
                {
                    var instanceStartTZ = TimeZoneInfo.FindSystemTimeZoneById(cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventTimezone)));
                    var eventStartDate = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(cur.GetLong(eventsProjection.IndexOf(CalendarContract.Instances.Begin))), instanceStartTZ);
                    var instanceEndTZ = TimeZoneInfo.FindSystemTimeZoneById(!string.IsNullOrEmpty(cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventEndTimezone))) ? cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventEndTimezone)) : cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventTimezone)));
                    var eventEndDate = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(cur.GetLong(eventsProjection.IndexOf(CalendarContract.Instances.End))), instanceEndTZ);

                    instances.Add(new CalendarEvent()
                    {
                        Id = cur.GetString(eventsProjection.IndexOf(CalendarContract.Instances.EventId)),
                        CalendarId = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.CalendarId)),
                        Title = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Title)),
                        StartDate = eventStartDate,
                        EndDate = eventEndDate
                    });
                }
            }
            if (!instances.Any() && !string.IsNullOrEmpty(calendarId))
            {
                // Make sure this calendar exists by testing retrieval
                try
                {
                    GetCalendarById(calendarId);
                }
                catch (Exception)
                {
                    throw new ArgumentOutOfRangeException($"[Android]: No calendar exists with the Id {calendarId}");
                }
            }
            return instances;
        }

        static Calendar GetCalendarById(string calendarId)
        {
            var calendarsUri = CalendarContract.Calendars.ContentUri;
            var calendarsProjection = new List<string>
            {
                CalendarContract.Calendars.InterfaceConsts.Id,
                CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName
            };

            // Android event ids are always integers
            if (!int.TryParse(calendarId, out var resultId))
            {
                throw new ArgumentException($"[Android]: No Event found for event Id {calendarId}");
            }

            var queryConditions = $"{CalendarContract.Calendars.InterfaceConsts.Deleted} != 1 {andCondition} {CalendarContract.Calendars.InterfaceConsts.Id} = {resultId}";

            using (var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(calendarsUri, calendarsProjection.ToArray(), queryConditions, null, null))
            {
                if (cur.Count > 0)
                {
                    cur.MoveToNext();
                    return new Calendar()
                    {
                        Id = cur.GetString(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.Id)),
                        Name = cur.GetString(calendarsProjection.IndexOf(CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName)),
                    };
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"[Android]: No calendar exists with the Id {calendarId}");
                }
            }
        }

        static async Task<CalendarEvent> PlatformGetEventByIdAsync(string eventId)
        {
            await Permissions.RequestAsync<Permissions.CalendarRead>();

            var eventsUri = CalendarContract.Events.ContentUri;
            var eventsProjection = new List<string>
            {
                CalendarContract.Events.InterfaceConsts.Id,
                CalendarContract.Events.InterfaceConsts.CalendarId,
                CalendarContract.Events.InterfaceConsts.Title,
                CalendarContract.Events.InterfaceConsts.Description,
                CalendarContract.Events.InterfaceConsts.EventLocation,
                CalendarContract.Events.InterfaceConsts.CustomAppUri,
                CalendarContract.Events.InterfaceConsts.AllDay,
                CalendarContract.Events.InterfaceConsts.Dtstart,
                CalendarContract.Events.InterfaceConsts.Dtend,
                CalendarContract.Events.InterfaceConsts.Rrule,
                CalendarContract.Events.InterfaceConsts.Rdate,
                CalendarContract.Events.InterfaceConsts.Organizer,
                CalendarContract.Events.InterfaceConsts.EventTimezone,
                CalendarContract.Events.InterfaceConsts.EventEndTimezone,
            };

            // Android event ids are always integers
            if (!int.TryParse(eventId, out var resultId))
            {
                throw new ArgumentException($"[Android]: No Event found for event Id {eventId}");
            }

            var calendarSpecificEvent = $"{CalendarContract.Events.InterfaceConsts.Id}={resultId}";
            using (var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(eventsUri, eventsProjection.ToArray(), calendarSpecificEvent, null, null))
            {
                if (cur.Count > 0)
                {
                    cur.MoveToNext();
                    var instanceStartTZ = TimeZoneInfo.FindSystemTimeZoneById(cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventTimezone)));
                    var eventStartDate = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtstart))), instanceStartTZ);
                    var instanceEndTZ = TimeZoneInfo.FindSystemTimeZoneById(!string.IsNullOrEmpty(cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventEndTimezone))) ? cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventEndTimezone)) : cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventTimezone)));
                    var eventEndDate = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(cur.GetLong(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Dtend))), instanceEndTZ);

                    var eventResult = new CalendarEvent
                    {
                        Id = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Id)),
                        CalendarId = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.CalendarId)),
                        Title = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Title)),
                        Description = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Description)),
                        Location = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventLocation)),
                        Url = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.CustomAppUri)),
                        StartDate = eventStartDate,
                        EndDate = eventEndDate,
                        Attendees = GetAttendeesForEvent(eventId, cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Organizer))),
                        RecurrancePattern = !string.IsNullOrEmpty(cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Rrule))) ? GetRecurranceRuleForEvent(cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Rrule))) : null,
                        Reminders = GetRemindersForEvent(eventId)
                    };
                    return eventResult;
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"[Android]: No Event found for event Id {eventId}");
                }
            }
        }

        static async Task<CalendarEvent> PlatformGetEventInstanceByIdAsync(string eventId, DateTimeOffset instance)
        {
            await Permissions.RequestAsync<Permissions.CalendarRead>();

            var eventsProjection = new List<string>
            {
                CalendarContract.Instances.EventId,
                CalendarContract.Instances.Begin,
                CalendarContract.Instances.End,
                CalendarContract.Events.InterfaceConsts.CalendarId,
                CalendarContract.Events.InterfaceConsts.Title,
                CalendarContract.Events.InterfaceConsts.Description,
                CalendarContract.Events.InterfaceConsts.EventLocation,
                CalendarContract.Events.InterfaceConsts.CustomAppUri,
                CalendarContract.Events.InterfaceConsts.AllDay,
                CalendarContract.Events.InterfaceConsts.Dtstart,
                CalendarContract.Events.InterfaceConsts.Dtend,
                CalendarContract.Events.InterfaceConsts.Rrule,
                CalendarContract.Events.InterfaceConsts.Rdate,
                CalendarContract.Events.InterfaceConsts.Organizer,
                CalendarContract.Events.InterfaceConsts.EventTimezone,
                CalendarContract.Events.InterfaceConsts.EventEndTimezone,
            };
            var instanceUriBuilder = CalendarContract.Instances.ContentUri.BuildUpon();
            ContentUris.AppendId(instanceUriBuilder, instance.AddDays(-1).AddMilliseconds(instance.Offset.TotalMilliseconds).ToUnixTimeMilliseconds());
            ContentUris.AppendId(instanceUriBuilder, instance.AddMilliseconds(instance.Offset.TotalMilliseconds).ToUnixTimeMilliseconds());

            var instancesUri = instanceUriBuilder.Build();
            var calendarSpecificEvent = $"{CalendarContract.Instances.EventId} = {eventId}";

            using (var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(instancesUri, eventsProjection.ToArray(), calendarSpecificEvent, null, $"{CalendarContract.Instances.Begin} ASC"))
            {
                if (cur.MoveToFirst())
                {
                    var instanceStartTZ = TimeZoneInfo.FindSystemTimeZoneById(cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventTimezone)));
                    var eventStartDate = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(cur.GetLong(eventsProjection.IndexOf(CalendarContract.Instances.Begin))), instanceStartTZ);
                    var instanceEndTZ = TimeZoneInfo.FindSystemTimeZoneById(!string.IsNullOrEmpty(cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventEndTimezone))) ? cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventEndTimezone)) : cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventTimezone)));
                    var eventEndDate = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(cur.GetLong(eventsProjection.IndexOf(CalendarContract.Instances.End))), instanceEndTZ);
                    return new CalendarEvent
                    {
                        Id = cur.GetString(eventsProjection.IndexOf(CalendarContract.Instances.EventId)),
                        CalendarId = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.CalendarId)),
                        Title = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Title)),
                        Description = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Description)),
                        Location = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.EventLocation)),
                        Url = cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.CustomAppUri)),
                        StartDate = eventStartDate,
                        EndDate = eventEndDate,
                        Attendees = GetAttendeesForEvent(eventId, cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Organizer))),
                        RecurrancePattern = !string.IsNullOrEmpty(cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Rrule))) ? GetRecurranceRuleForEvent(cur.GetString(eventsProjection.IndexOf(CalendarContract.Events.InterfaceConsts.Rrule))) : null,
                        Reminders = GetRemindersForEvent(eventId)
                    };
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"[Android]: No Event found for event Id {eventId}");
                }
            }
        }

        static IEnumerable<CalendarEventAttendee> GetAttendeesForEvent(string eventId, string organizer)
        {
            var attendeesUri = CalendarContract.Attendees.ContentUri;
            var attendeesProjection = new List<string>
            {
                CalendarContract.Attendees.InterfaceConsts.EventId,
                CalendarContract.Attendees.InterfaceConsts.AttendeeEmail,
                CalendarContract.Attendees.InterfaceConsts.AttendeeName,
                CalendarContract.Attendees.InterfaceConsts.AttendeeType
            };
            var attendeeSpecificAttendees = $"{CalendarContract.Attendees.InterfaceConsts.EventId}={eventId}";
            var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(attendeesUri, attendeesProjection.ToArray(), attendeeSpecificAttendees, null, null);
            var attendees = new List<CalendarEventAttendee>();
            while (cur.MoveToNext())
            {
                attendees.Add(new CalendarEventAttendee()
                {
                    Name = cur.GetString(attendeesProjection.IndexOf(CalendarContract.Attendees.InterfaceConsts.AttendeeName)),
                    Email = cur.GetString(attendeesProjection.IndexOf(CalendarContract.Attendees.InterfaceConsts.AttendeeEmail)),
                    Type = (AttendeeType)cur.GetInt(attendeesProjection.IndexOf(CalendarContract.Attendees.InterfaceConsts.AttendeeType)),
                    IsOrganizer = cur.GetString(attendeesProjection.IndexOf(CalendarContract.Attendees.InterfaceConsts.AttendeeEmail)) == organizer
                });
            }
            cur.Dispose();
            return attendees.OrderByDescending(x => x.IsOrganizer);
        }

        static IEnumerable<CalendarEventReminder> GetRemindersForEvent(string eventId)
        {
            var remindersUri = CalendarContract.Reminders.ContentUri;
            var remindersProjection = new List<string>
            {
                CalendarContract.Reminders.InterfaceConsts.EventId,
                CalendarContract.Reminders.InterfaceConsts.Minutes
            };
            var remindersSpecificAttendees = $"{CalendarContract.Reminders.InterfaceConsts.EventId}={eventId}";
            var reminders = new List<CalendarEventReminder>();
            using (var cur = Platform.AppContext.ApplicationContext.ContentResolver.Query(remindersUri, remindersProjection.ToArray(), remindersSpecificAttendees, null, null))
            {
                while (cur.MoveToNext())
                {
                    reminders.Add(new CalendarEventReminder()
                    {
                        MinutesPriorToEventStart = cur.GetInt(remindersProjection.IndexOf(CalendarContract.Reminders.InterfaceConsts.Minutes))
                    });
                }
            }
            return reminders;
        }

        static async Task<string> PlatformCreateCalendar(Calendar newCalendar)
        {
            await Permissions.RequestAsync<Permissions.CalendarWrite>();

            var calendarUri = CalendarContract.Calendars.ContentUri;
            var cursor = Platform.AppContext.ApplicationContext.ContentResolver;
            var calendarValues = new ContentValues();
            calendarValues.Put(CalendarContract.Calendars.InterfaceConsts.AccountName, "Xamarin.Essentials.Calendar");
            calendarValues.Put(CalendarContract.Calendars.InterfaceConsts.AccountType, CalendarContract.AccountTypeLocal);
            calendarValues.Put(CalendarContract.Calendars.Name, newCalendar.Name);
            calendarValues.Put(CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName, newCalendar.Name);
            calendarValues.Put(CalendarContract.Calendars.InterfaceConsts.CalendarAccessLevel, CalendarAccess.AccessOwner.ToString());
            calendarValues.Put(CalendarContract.Calendars.InterfaceConsts.Visible, true);
            calendarValues.Put(CalendarContract.Calendars.InterfaceConsts.SyncEvents, true);
            calendarUri = calendarUri.BuildUpon()
                    .AppendQueryParameter(CalendarContract.CallerIsSyncadapter, "true")
                    .AppendQueryParameter(CalendarContract.Calendars.InterfaceConsts.AccountName, "Xamarin.Essentials.Calendar")
                    .AppendQueryParameter(CalendarContract.Calendars.InterfaceConsts.AccountType, CalendarContract.AccountTypeLocal)
                    .Build();
            var result = cursor.Insert(calendarUri, calendarValues);
            return result.ToString();
        }

        static async Task<string> PlatformCreateCalendarEvent(CalendarEvent newEvent)
        {
            await Permissions.RequestAsync<Permissions.CalendarWrite>();

            var result = 0;
            if (string.IsNullOrEmpty(newEvent.CalendarId))
            {
                return string.Empty;
            }
            var eventUri = CalendarContract.Events.ContentUri;
            var eventValues = SetupContentValues(newEvent);

            var resultUri = Platform.AppContext.ApplicationContext.ContentResolver.Insert(eventUri, eventValues);
            if (int.TryParse(resultUri.LastPathSegment, out result))
            {
                return result.ToString();
            }
            throw new ArgumentException("[Android]: Could not create appointment with supplied parameters");
        }

        static async Task<bool> PlatformUpdateCalendarEvent(CalendarEvent eventToUpdate)
        {
            await Permissions.RequestAsync<Permissions.CalendarWrite>();

            var thisEvent = await GetEventByIdAsync(eventToUpdate.Id);

            var eventUri = CalendarContract.Events.ContentUri;
            var eventValues = SetupContentValues(eventToUpdate);

            if (string.IsNullOrEmpty(eventToUpdate.CalendarId) || thisEvent == null)
            {
                return false;
            }
            else if (thisEvent.CalendarId != eventToUpdate.CalendarId)
            {
                await DeleteCalendarEventById(thisEvent.Id, thisEvent.CalendarId);
                var resultUri = Platform.AppContext.ApplicationContext.ContentResolver.Insert(eventUri, eventValues);
                if (int.TryParse(resultUri.LastPathSegment, out var result))
                {
                    return true;
                }
            }
            else if (Platform.AppContext.ApplicationContext.ContentResolver.Update(eventUri, eventValues, $"{CalendarContract.Attendees.InterfaceConsts.Id}={eventToUpdate.Id}", null) > 0)
            {
                return true;
            }
            throw new ArgumentException("[Android]: Could not update appointment with supplied parameters");
        }

        static ContentValues SetupContentValues(CalendarEvent newEvent)
        {
            var eventValues = new ContentValues();
            eventValues.Put(CalendarContract.Events.InterfaceConsts.CalendarId, newEvent.CalendarId);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Title, newEvent.Title);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Description, newEvent.Description);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.EventLocation, newEvent.Location);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.CustomAppUri, newEvent.Url);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.AllDay, newEvent.AllDay);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Dtstart, newEvent.StartDate.ToUnixTimeMilliseconds().ToString());
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Dtend, newEvent.EndDate.HasValue ? newEvent.EndDate.Value.ToUnixTimeMilliseconds().ToString() : newEvent.StartDate.AddDays(1).ToUnixTimeMilliseconds().ToString());
            eventValues.Put(CalendarContract.Events.InterfaceConsts.EventTimezone, TimeZoneInfo.Local.Id);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.EventEndTimezone, TimeZoneInfo.Local.Id);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Deleted, 0);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Rrule, newEvent.RecurrancePattern.ConvertRule());
            return eventValues;
        }

        static async Task<bool> PlatformDeleteCalendarEventInstanceByDate(string eventId, string calendarId, DateTimeOffset dateOfInstanceUtc)
        {
            await Permissions.RequestAsync<Permissions.CalendarWrite>();

            var thisEvent = await GetEventInstanceByIdAsync(eventId, dateOfInstanceUtc);

            var eventUri = ContentUris.WithAppendedId(CalendarContract.Events.ContentExceptionUri, long.Parse(eventId));

            var eventValues = new ContentValues();
            eventValues.Put(CalendarContract.Events.InterfaceConsts.OriginalInstanceTime, thisEvent.StartDate.ToUnixTimeMilliseconds());
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Status, (int)EventsStatus.Canceled);

            var resultUri = Platform.AppContext.ApplicationContext.ContentResolver.Insert(eventUri, eventValues);
            if (int.TryParse(resultUri.LastPathSegment, out var result))
            {
                return result > 0;
            }
            return false;
        }

        static async Task<bool> PlatformDeleteCalendarEventById(string eventId, string calendarId)
        {
            await Permissions.RequestAsync<Permissions.CalendarWrite>();

            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentException("[Android]: You must supply an event id to delete an event.");
            }

            var calendarEvent = await GetEventByIdAsync(eventId);

            if (calendarEvent.CalendarId != calendarId)
            {
                throw new ArgumentOutOfRangeException("[Android]: Supplied event does not belong to supplied calendar");
            }

            var eventUri = ContentUris.WithAppendedId(CalendarContract.Events.ContentUri, long.Parse(eventId));
            var result = Platform.AppContext.ApplicationContext.ContentResolver.Delete(eventUri, null, null);

            return result > 0;
        }

        static async Task<bool> PlatformAddAttendeeToEvent(CalendarEventAttendee newAttendee, string eventId)
        {
            await Permissions.RequestAsync<Permissions.CalendarWrite>();

            var calendarEvent = await GetEventByIdAsync(eventId);

            if (calendarEvent == null)
            {
                throw new ArgumentException("[Android]: You must supply a valid event id to add an attendee to an event.");
            }

            var attendeeUri = CalendarContract.Attendees.ContentUri;
            var attendeeValues = new ContentValues();

            attendeeValues.Put(CalendarContract.Attendees.InterfaceConsts.EventId, eventId);
            attendeeValues.Put(CalendarContract.Attendees.InterfaceConsts.AttendeeEmail, newAttendee.Email);
            attendeeValues.Put(CalendarContract.Attendees.InterfaceConsts.AttendeeName, newAttendee.Name);
            attendeeValues.Put(CalendarContract.Attendees.InterfaceConsts.AttendeeType, (int)newAttendee.Type);

            var resultUri = Platform.AppContext.ApplicationContext.ContentResolver.Insert(attendeeUri, attendeeValues);
            var result = Convert.ToInt32(resultUri.LastPathSegment);

            return result > 0;
        }

        static async Task<bool> PlatformRemoveAttendeeFromEvent(CalendarEventAttendee newAttendee, string eventId)
        {
            await Permissions.RequestAsync<Permissions.CalendarWrite>();

            var calendarEvent = await GetEventByIdAsync(eventId);

            if (calendarEvent == null)
            {
                throw new ArgumentException("[Android]: You must supply a valid event id to remove an attendee from an event.");
            }

            var attendeesUri = CalendarContract.Attendees.ContentUri;
            var attendeeSpecificAttendees = $"{CalendarContract.Attendees.InterfaceConsts.AttendeeName}='{newAttendee.Name}' {andCondition} ";
            attendeeSpecificAttendees += $"{CalendarContract.Attendees.InterfaceConsts.AttendeeEmail}='{newAttendee.Email}'";

            var result = Platform.AppContext.ApplicationContext.ContentResolver.Delete(attendeesUri, attendeeSpecificAttendees, null);

            return result > 0;
        }

        // https://icalendar.org/iCalendar-RFC-5545/3-8-5-3-recurrence-rule.html
        static RecurrenceRule GetRecurranceRuleForEvent(string rule)
        {
            var recurranceRule = new RecurrenceRule();
            if (rule.Contains("FREQ="))
            {
                var ruleFrequency = rule.Substring(rule.IndexOf("FREQ=", StringComparison.Ordinal) + 5);
                ruleFrequency = ruleFrequency.Contains(";") ? ruleFrequency.Substring(0, ruleFrequency.IndexOf(";")) : ruleFrequency;
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
                ruleInterval = ruleInterval.Contains(";") ? ruleInterval.Substring(0, ruleInterval.IndexOf(";", StringComparison.Ordinal)) : ruleInterval;
                recurranceRule.Interval = uint.Parse(ruleInterval);
            }
            else
            {
                recurranceRule.Interval = 1;
            }

            if (rule.Contains("COUNT="))
            {
                var ruleOccurences = rule.Substring(rule.IndexOf("COUNT=", StringComparison.Ordinal) + 6);
                ruleOccurences = ruleOccurences.Contains(";") ? ruleOccurences.Substring(0, ruleOccurences.IndexOf(";", StringComparison.Ordinal)) : ruleOccurences;
                recurranceRule.TotalOccurrences = uint.Parse(ruleOccurences);
            }

            if (rule.Contains("UNTIL="))
            {
                var ruleEndDate = rule.Substring(rule.IndexOf("UNTIL=", StringComparison.Ordinal) + 6);
                ruleEndDate = ruleEndDate.Contains(";") ? ruleEndDate.Substring(0, ruleEndDate.IndexOf(";", StringComparison.Ordinal)) : ruleEndDate;
                recurranceRule.EndDate = DateTimeOffset.ParseExact(ruleEndDate.Replace("T", string.Empty).Replace("Z", string.Empty), "yyyyMMddHHmmss", null);
            }

            if (rule.Contains("BYDAY="))
            {
                var ruleOccurenceDays = rule.Substring(rule.IndexOf("BYDAY=", StringComparison.Ordinal) + 6);
                ruleOccurenceDays = ruleOccurenceDays.Contains(";") ? ruleOccurenceDays.Substring(0, ruleOccurenceDays.IndexOf(";", StringComparison.Ordinal)) : ruleOccurenceDays;
                recurranceRule.DaysOfTheWeek = new List<DayOfTheWeek>();
                foreach (var d in ruleOccurenceDays.Split(','))
                {
                    var day = d;
                    if (d.Any(char.IsDigit))
                    {
                        var regex = new Regex(@"[-]?\d+");
                        var iterationOffset = regex.Match(d);

                        day = d.Substring(iterationOffset.Index + iterationOffset.Length);

                        if (recurranceRule.Frequency == RecurrenceFrequency.Monthly)
                        {
                            recurranceRule.Frequency = RecurrenceFrequency.MonthlyOnDay;
                        }
                        else
                        {
                            recurranceRule.Frequency = RecurrenceFrequency.YearlyOnDay;
                        }
                        recurranceRule.WeekOfMonth = (IterationOffset)Convert.ToInt32(iterationOffset.Value);
                    }
                    switch (day)
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

            if (rule.Contains("BYMONTHDAY="))
            {
                var ruleOccurenceMonthDays = rule.Substring(rule.IndexOf("BYMONTHDAY=", StringComparison.Ordinal) + 11);
                ruleOccurenceMonthDays = ruleOccurenceMonthDays.Contains(";") ? ruleOccurenceMonthDays.Substring(0, ruleOccurenceMonthDays.IndexOf(";", StringComparison.Ordinal)) : ruleOccurenceMonthDays;
                recurranceRule.DayOfTheMonth = (uint)Math.Abs(Convert.ToInt32(ruleOccurenceMonthDays.Split(',').FirstOrDefault()));
            }

            if (rule.Contains("BYMONTH="))
            {
                var ruleOccurenceMonths = rule.Substring(rule.IndexOf("BYMONTH=", StringComparison.Ordinal) + 8);
                ruleOccurenceMonths = ruleOccurenceMonths.Contains(";") ? ruleOccurenceMonths.Substring(0, ruleOccurenceMonths.IndexOf(";", StringComparison.Ordinal)) : ruleOccurenceMonths;
                recurranceRule.MonthOfTheYear = (MonthOfYear)Convert.ToUInt32(ruleOccurenceMonths.Split(',').FirstOrDefault());
            }

            if (rule.Contains("BYSETPOS="))
            {
                var ruleDayIterationOffset = rule.Substring(rule.IndexOf("BYSETPOS=", StringComparison.Ordinal) + 9);
                ruleDayIterationOffset = ruleDayIterationOffset.Contains(";") ? ruleDayIterationOffset.Substring(0, ruleDayIterationOffset.IndexOf(";", StringComparison.Ordinal)) : ruleDayIterationOffset;
                recurranceRule.WeekOfMonth = (IterationOffset)Convert.ToInt32(ruleDayIterationOffset.Split(',').FirstOrDefault());

                if (recurranceRule.Frequency == RecurrenceFrequency.Monthly)
                {
                    recurranceRule.Frequency = RecurrenceFrequency.MonthlyOnDay;
                }
                else
                {
                    recurranceRule.Frequency = RecurrenceFrequency.YearlyOnDay;
                }
            }
            return recurranceRule;
        }

        static string ConvertRule(this RecurrenceRule recurrenceRule)
        {
            var eventRecurrence = string.Empty;

            switch (recurrenceRule.Frequency)
            {
                case RecurrenceFrequency.Daily:
                case RecurrenceFrequency.Weekly:
                    if (recurrenceRule.DaysOfTheWeek != null && recurrenceRule.DaysOfTheWeek.Count > 0)
                    {
                        eventRecurrence += $"FREQ={weeklyFrequency};";
                        eventRecurrence += $"BYDAY={recurrenceRule.DaysOfTheWeek.ToDayString()};";
                    }
                    else
                    {
                        eventRecurrence += $"FREQ={dailyFrequency};";
                    }
                    eventRecurrence += $"INTERVAL={recurrenceRule.Interval};";
                    break;
                case RecurrenceFrequency.Monthly:
                    eventRecurrence += $"FREQ={monthlyFrequency};";
                    if (recurrenceRule.DaysOfTheWeek != null && recurrenceRule.DaysOfTheWeek.Count > 0)
                    {
                        eventRecurrence += $"BYDAY={recurrenceRule.WeekOfMonth}{recurrenceRule.DaysOfTheWeek.ToDayString()};";
                    }
                    else if (recurrenceRule.DayOfTheMonth != 0)
                    {
                        eventRecurrence += $"BYMONTHDAY={recurrenceRule.DayOfTheMonth};";
                    }
                    else
                    {
                        eventRecurrence += $"INTERVAL={recurrenceRule.Interval};";
                    }
                    break;
                case RecurrenceFrequency.Yearly:
                    eventRecurrence += $"FREQ={yearlyFrequency};";
                    if (recurrenceRule.DaysOfTheWeek != null && recurrenceRule.DaysOfTheWeek.Count > 0)
                    {
                        eventRecurrence += $"BYMONTH={(int)recurrenceRule.MonthOfTheYear};";
                        eventRecurrence += $"BYDAY={recurrenceRule.WeekOfMonth}{recurrenceRule.DaysOfTheWeek.ToDayString()};";
                    }
                    else if (recurrenceRule.DayOfTheMonth != 0)
                    {
                        eventRecurrence += $"BYMONTH={(int)recurrenceRule.MonthOfTheYear};";
                        eventRecurrence += $"BYMONTHDAY={recurrenceRule.DayOfTheMonth};";
                    }
                    else
                    {
                        eventRecurrence += $"INTERVAL={recurrenceRule.Interval};";
                    }
                    break;
            }

            if (recurrenceRule.EndDate.HasValue)
            {
                eventRecurrence += $"UNTIL={recurrenceRule.EndDate.Value.ToUniversalTime():yyyyMMddTHHmmssZ};";
            }
            else if (recurrenceRule.TotalOccurrences.HasValue)
            {
                eventRecurrence += $"COUNT={recurrenceRule.TotalOccurrences.Value};";
            }

            return eventRecurrence.Substring(0, eventRecurrence.Length - 1);
        }

        static string ToShortString(this DayOfTheWeek day)
        {
            switch (day)
            {
                case DayOfTheWeek.Monday:
                    return "MO";
                case DayOfTheWeek.Tuesday:
                    return "TU";
                case DayOfTheWeek.Wednesday:
                    return "WE";
                case DayOfTheWeek.Thursday:
                    return "TH";
                case DayOfTheWeek.Friday:
                    return "FR";
                case DayOfTheWeek.Saturday:
                    return "SA";
                case DayOfTheWeek.Sunday:
                    return "SU";
            }
            return "INVALID";
        }

        static string ToDayString(this List<DayOfTheWeek> dayList)
        {
            var toReturn = string.Empty;
            foreach (var d in dayList)
            {
                toReturn += d.ToShortString() + ",";
            }
            return toReturn.Substring(0, toReturn.Length - 1);
        }
    }
}
