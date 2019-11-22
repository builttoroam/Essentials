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
        static bool PlatformIsSupported => true;

        static async Task PlatformRequestCalendarReadAccess() => await Permissions.RequireAsync(PermissionType.CalendarRead);

        static async Task PlatformRequestCalendarWriteAccess() => await Permissions.RequireAsync(PermissionType.CalendarWrite);

        static async Task<List<DeviceCalendar>> PlatformGetCalendarsAsync()
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
            var calendarList = new List<DeviceCalendar>();

            foreach (var t in calendars)
            {
                calendarList.Add(new DeviceCalendar
                {
                    Id = t.CalendarIdentifier,
                    Name = t.Title,
                    IsReadOnly = !t.AllowsContentModifications
                });
            }
            return calendarList;
        }

        static async Task<List<DeviceEvent>> PlatformGetEventsAsync(string calendarId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            await Permissions.RequireAsync(PermissionType.CalendarRead);

            var eventList = new List<DeviceEvent>();
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
                throw new Exception($"iOS: Unexpected null reference exception {ex.Message}");
            }

            var query = CalendarRequest.Instance.PredicateForEvents(sDate, eDate, calendars);
            var events = CalendarRequest.Instance.EventsMatching(query);

            foreach (var e in events)
            {
                eventList.Add(new DeviceEvent
                {
                    Id = e.CalendarItemIdentifier,
                    CalendarId = e.Calendar.CalendarIdentifier,
                    Title = e.Title,
                    StartDate = e.StartDate.ToDateTimeOffset(),
                    EndDate = e.EndDate.ToDateTimeOffset()
                });
            }
            eventList.Sort((x, y) =>
            {
                if (!x.StartDate.HasValue)
                {
                    if (!y.EndDate.HasValue)
                    {
                        return 0;
                    }
                    return -1;
                }
                if (!y.EndDate.HasValue)
                {
                    return 1;
                }
                return x.StartDate.Value.CompareTo(y.EndDate.Value);
            });

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
                EndDate = e.EndDate.ToDateTimeOffset(),
                AllDay = e.AllDay,
                Attendees = e.Attendees != null ? GetAttendeesForEvent(e.Attendees) : new List<DeviceEventAttendee>()
            };
        }

        static List<DeviceEventAttendee> GetAttendeesForEvent(IList<EKParticipant> inviteList)
        {
            var attendees = new List<DeviceEventAttendee>();

            foreach (var attendee in inviteList)
            {
                attendees.Add(new DeviceEventAttendee()
                {
                    Name = attendee.Name,
                    Email = attendee.Name
                });
            }
            return attendees;
        }
    }
}
