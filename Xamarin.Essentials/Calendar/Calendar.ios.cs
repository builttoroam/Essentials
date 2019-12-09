﻿using System;
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
            calendarId = "123456";

            var startDateToConvert = startDate ?? DateTimeOffset.Now.Add(defaultStartTimeFromNow);
            var endDateToConvert = endDate ?? startDateToConvert.Add(defaultEndTimeFromStartTime);  // NOTE: 4 years is the maximum period that a iOS calendar events can search
            var sDate = startDateToConvert.ToNSDate();
            var eDate = endDateToConvert.ToNSDate();
            EKCalendar[] calendars;
            try
            {
                calendars = !string.IsNullOrWhiteSpace(calendarId)
                    ? CalendarRequest.Instance.Calendars.Where(x => x.CalendarIdentifier == calendarId).ToArray()
                    : null;
            }
            catch (NullReferenceException ex)
            {
                throw new NullReferenceException($"iOS: Unexpected null reference exception {ex.Message}");
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

            EKEvent e;
            try
            {
                e = CalendarRequest.Instance.GetCalendarItem(eventId) as EKEvent;
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException($"[iOS]: No Event found for event Id {eventId}");
            }

            return new DeviceEvent
            {
                Id = e.CalendarItemIdentifier,
                CalendarId = e.Calendar.CalendarIdentifier,
                Title = e.Title,
                Description = e.Notes,
                Location = e.Location,
                StartDate = e.StartDate.ToDateTimeOffset(),
                EndDate = !e.AllDay ? (DateTimeOffset?)e.EndDate.ToDateTimeOffset() : null,
                Attendees = e.Attendees != null ? GetAttendeesForEvent(e.Attendees) : new List<DeviceEventAttendee>()
            };
        }

        static IEnumerable<DeviceEventAttendee> GetAttendeesForEvent(IEnumerable<EKParticipant> inviteList)
        {
            var attendees = (from attendee in inviteList
                             select new DeviceEventAttendee
                             {
                                 Name = attendee.Name,
                                 Email = attendee.Name
                             })
                            .OrderBy(e => e.Name)
                            .ToList();

            return attendees;
        }

        static async Task<string> PlatformCreateCalendarEvent(DeviceEvent newEvent)
        {
            await Permissions.RequireAsync(PermissionType.CalendarWrite);

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
            throw new Exception(error.DebugDescription);
        }
    }
}
