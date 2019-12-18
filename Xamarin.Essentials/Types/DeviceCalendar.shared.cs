using System;
using System.Collections.Generic;

namespace Xamarin.Essentials
{
    [Preserve(AllMembers = true)]
    public class DeviceCalendar
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public bool IsReadOnly { get; set; }
    }

    [Preserve(AllMembers = true)]
    public class DeviceEvent
    {
        public string Id { get; set; }

        public string CalendarId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Location { get; set; }

        public bool AllDay
        {
            get => !EndDate.HasValue;
            set
            {
                if (value)
                {
                    EndDate = null;
                }
                else
                {
                    EndDate = StartDate;
                }
            }
        }

        public DateTimeOffset StartDate { get; set; }

        public TimeSpan? Duration
        {
            get => EndDate.HasValue ? EndDate - StartDate : null;
            set
            {
                if (value.HasValue)
                {
                    EndDate = StartDate.Add(value.Value);
                }
                else
                {
                    EndDate = null;
                }
            }
        }

        public DateTimeOffset? EndDate { get; set; }

        public IEnumerable<DeviceEventAttendee> Attendees { get; set; }

        public RecurrenceRule RecurrancePattern { get; set; }
    }

    [Preserve(AllMembers = true)]
    public class DeviceEventAttendee
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public bool Required { get; set; }
    }

    [Preserve(AllMembers = true)]
    public class RecurrenceRule
    {
        public uint? TotalOccurences { get; set; }

        public uint Interval { get; set; }

        public DateTimeOffset? EndDate { get; set; }

        public RecurrenceFrequency Frequency { get; set; }

        // Only allow event to occur on these days [not available for daily]
        public List<DayOfTheWeek> DaysOfTheWeek { get; set; }

        public List<int> DaysOfTheMonth { get; set; }

        public List<int> WeeksOfTheYear { get; set; }

        public List<int> MonthsOfTheYear { get; set; }

        public List<int> DaysOfTheYear { get; set; }

        public List<int> DayIterationOffSetPosition { get; set; }

        public DayOfTheWeek StartOfTheWeek { get; set; }
    }

    public enum RecurrenceFrequency
    {
        Daily = 0,
        Weekly = 1,
        Monthly = 2,
        Yearly = 3
    }

    public enum DayOfTheWeek
    {
        NotSet = 0,
        Sunday = 1,
        Monday = 2,
        Tuesday = 3,
        Wednesday = 4,
        Thursday = 5,
        Friday = 6,
        Saturday = 7
    }
}
