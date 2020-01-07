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

        public IEnumerable<DeviceEventReminder> Reminders { get; set; }

        public RecurrenceRule RecurrancePattern { get; set; }
    }

    [Preserve(AllMembers = true)]
    public class DeviceEventReminder
    {
        public int MinutesPriorToEventStart { get; set; }
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

        public List<MonthOfTheYear> MonthsOfTheYear { get; set; }

        public List<int> DaysOfTheYear { get; set; }

        public List<IterationOffset> DayIterationOffSetPosition { get; set; }

        public DayOfTheWeek StartOfTheWeek { get; set; }

        public override string ToString()
        {
            var toReturn = $"Occurs ";

            if (Interval > 0)
            {
                if (Interval == 1)
                {
                    toReturn += $"Every ";
                }
                else
                {
                    toReturn += $"Every {((int)Interval).ToOrdinal()} ";
                }
                switch (Frequency)
                {
                    case RecurrenceFrequency.Daily:
                        toReturn += "Day ";
                        break;
                    case RecurrenceFrequency.Weekly:
                        toReturn += "Week ";
                        break;
                    case RecurrenceFrequency.Monthly:
                        toReturn += "Month ";
                        break;
                    case RecurrenceFrequency.Yearly:
                        toReturn += "Year ";
                        break;
                }
            }

            if (DaysOfTheWeek?.Count > 0)
            {
                toReturn += $"On: [";
                foreach (var d in DaysOfTheWeek)
                {
                    toReturn += $"{d}, ";
                }
                toReturn = toReturn.Substring(0, toReturn.Length - 2) + "] ";
            }

            if (DaysOfTheMonth?.Count > 0)
            {
                toReturn += $"on the: [";
                foreach (var d in DaysOfTheMonth)
                {
                    toReturn += $"{d.ToOrdinal()}, ";
                }
                toReturn = toReturn.Substring(0, toReturn.Length - 2) + "] of the month ";
            }

            if (DaysOfTheYear?.Count > 0)
            {
                toReturn += $"On: [";
                foreach (var d in DaysOfTheYear)
                {
                    toReturn += $"{d.ToOrdinal()}, ";
                }
                toReturn = toReturn.Substring(0, toReturn.Length - 2) + "] of the year ";
            }

            if (WeeksOfTheYear?.Count > 0)
            {
                toReturn += $"Inclding every: [";
                foreach (var d in WeeksOfTheYear)
                {
                    toReturn += $"{d.ToOrdinal()}, ";
                }
                toReturn = toReturn.Substring(0, toReturn.Length - 2) + "] week of the year ";
            }

            if (DayIterationOffSetPosition?.Count > 0 && (Frequency == RecurrenceFrequency.MonthlyOnDay || Frequency == RecurrenceFrequency.YearlyOnDay))
            {
                toReturn += $"Occuring on the: [";
                foreach (var d in DayIterationOffSetPosition)
                {
                    toReturn += $"{d}, ";
                }
                toReturn = toReturn.Substring(0, toReturn.Length - 2) + "] of each month ";
            }

            if (TotalOccurences > 0)
            {
                toReturn += $"For the next {TotalOccurences} occurences ";
            }

            if (EndDate.HasValue)
            {
                toReturn += $"Until {EndDate.Value.DateTime.ToShortDateString()} ";
            }

            return toReturn;
        }
    }

    public enum RecurrenceFrequency
    {
        None = -1,
        Daily = 0,
        Weekly = 1,
        Monthly = 2,
        MonthlyOnDay = 3,
        Yearly = 4,
        YearlyOnDay = 5
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

    public enum MonthOfTheYear
    {
        NotSet = 0,
        January = 1,
        February = 2,
        March = 3,
        April = 4,
        May = 5,
        June = 6,
        July = 7,
        August = 8,
        September = 9,
        October = 10,
        November = 11,
        December = 12
    }

    public enum IterationOffset
    {
        NotSet = -1,
        First = 0,
        Second = 1,
        Third = 2,
        Fourth = 3,
        Last = 4
    }
}
