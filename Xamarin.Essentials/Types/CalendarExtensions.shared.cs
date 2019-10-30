using System;
using System.Collections.Generic;

namespace Xamarin.Essentials
{
    public class CalendarObject
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public bool IsReadOnly { get; set; }

        // public List<Event> EventList { get; set; }
    }

    public class Event
    {
        public int Id { get; set; }

        // Calendar Id this event is for
        public string CalendarId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Location { get; set; }

        public bool AllDay { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public bool HasRecurrence { get; set; }

        public RecurrenceRule RecurrenceFrequency { get; set; }

        public bool HasReminder { get; set; }

        public List<Reminder> Reminders { get; set; }
    }

    public class Reminder
    {
        public static int Id { get; set; }

        public int MinutesPriorToEvent { get; set; }
    }

    public class RecurrenceRule
    {
        public int TotalOccurences { get; set; }

        public int Interval { get; set; }

        public DateTime EndDate { get; set; }

        public RecurrenceFrequency Frequency { get; set; }

        // Only allow event to occur on these days [not available for daily]
        public List<DayOfTheWeek> DaysOfTheWeek { get; set; }

        public List<int> DaysOfTheMonth { get; set; }

        public List<int> WeeksOfTheYear { get; set; }

        public List<int> SetPositions { get; set; }
    }

    public class Attendee
    {
        public string Name { get; set; }

        public string Email { get; set; }

        // public IAttendeeDetails AttendeeDetails => DeviceSpecificAttendeeDetails;
    }

    public interface IAttendeeDetails
    {
    }

    public enum RecurrenceFrequency
    {
        Daily,
        Weekly,
        Fortnightly,
        Monthly,
        Yearly
    }

    public enum DayOfTheWeek
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday
    }
}
