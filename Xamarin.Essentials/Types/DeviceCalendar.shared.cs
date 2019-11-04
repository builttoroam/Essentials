using System;
using System.Collections.Generic;

namespace Xamarin.Essentials
{
    [Preserve(AllMembers = true)]
    public class DeviceCalendar : ICalendar
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public bool IsReadOnly { get; set; }
    }

    public interface ICalendar
    {
        string Id { get; set; }

        string Name { get; set; }

        bool IsReadOnly { get; set; }
    }

    public interface IEvent
    {
        string Id { get; set; }

        string CalendarId { get; set; }

        string Title { get; set; }

        string Description { get; set; }

        string Location { get; set; }

        bool AllDay { get; set; }

        long? Start { get; set; }

        long? End { get; set; }

        bool HasAlarm { get; set; }

        bool HasAttendees { get; set; }

        bool HasExtendedProperties { get; set; }

        string Status { get; set; }

        RecurrenceRule RecurrancePattern { get; set; }
    }

    [Preserve(AllMembers = true)]
    public class Event : IEvent
    {
        public string Id { get; set; }

        public string CalendarId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Location { get; set; }

        public bool AllDay { get; set; }

        public long? Start { get; set; }

        public DateTimeOffset? StartDate => Start.HasValue ? (DateTimeOffset?)DateTimeOffset.FromUnixTimeMilliseconds((long)Start.Value).ToLocalTime() : null;

        public long? End { get; set; }

        public DateTimeOffset? EndDate => End.HasValue ? (DateTimeOffset?)DateTimeOffset.FromUnixTimeMilliseconds((long)End.Value).ToLocalTime() : null;

        public bool HasAlarm { get; set; }

        public bool HasAttendees { get; set; }

        public bool HasExtendedProperties { get; set; }

        public string Status { get; set; }

        RecurrenceRule IEvent.RecurrancePattern { get; set; }
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
