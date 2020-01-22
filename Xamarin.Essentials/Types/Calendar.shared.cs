﻿using System;
using System.Collections.Generic;

namespace Xamarin.Essentials
{
    [Preserve(AllMembers = true)]
    public class Calendar
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public bool IsReadOnly { get; set; }
    }

    [Preserve(AllMembers = true)]
    public class CalendarEvent
    {
        public string Id { get; set; }

        public string CalendarId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Location { get; set; }

        public bool AllDay
        {
            get => !EndDate.HasValue;
            set => EndDate = value ? (DateTimeOffset?)null : StartDate;
        }

        public DateTimeOffset StartDate { get; set; }

        public TimeSpan? Duration
        {
            get => EndDate.HasValue ? EndDate - StartDate : null;
            set => EndDate = value.HasValue ? StartDate.Add(value.Value) : (DateTimeOffset?)null;
        }

        public string Url { get; set; }

        public DateTimeOffset? EndDate { get; set; }

        public IEnumerable<CalendarEventAttendee> Attendees { get; set; }

        public IEnumerable<CalendarEventReminder> Reminders { get; set; }

        public RecurrenceRule RecurrancePattern { get; set; }
    }

    [Preserve(AllMembers = true)]
    public class CalendarEventAttendee
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public AttendeeType Type { get; set; }

        public bool IsOrganizer { get; set; }
    }

    [Preserve(AllMembers = true)]
    public class CalendarEventReminder
    {
        public int MinutesPriorToEventStart { get; set; }
    }

    [Preserve(AllMembers = true)]
    public class RecurrenceRule
    {
        public uint? TotalOccurrences { get; set; }

        public uint Interval { get; set; }

        public DateTimeOffset? EndDate { get; set; }

        public RecurrenceFrequency? Frequency { get; set; }

        // Only allow event to occur on these days [not available for daily]
        public List<DayOfTheWeek> DaysOfTheWeek { get; set; }

        public uint DayOfTheMonth { get; set; }

        public MonthOfYear? MonthOfTheYear { get; set; }

        public IterationOffset? WeekOfMonth { get; set; }
    }

    public enum RecurrenceFrequency
    {
        Daily = 0,
        Weekly = 1,
        Monthly = 2,
        MonthlyOnDay = 3,
        Yearly = 4,
        YearlyOnDay = 5
    }

    public enum DayOfTheWeek
    {
        Sunday = 1,
        Monday = 2,
        Tuesday = 3,
        Wednesday = 4,
        Thursday = 5,
        Friday = 6,
        Saturday = 7
    }

    public enum MonthOfYear
    {
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

#if __ANDROID__ || __IOS__
    public enum IterationOffset
    {
        Last = -1,
        First = 1,
        Second = 2,
        Third = 3,
        Fourth = 4,
    }

#else
    public enum IterationOffset
    {
        First = 0,
        Second = 1,
        Third = 2,
        Fourth = 3,
        Last = 4
    }

#endif
    public enum AttendeeType
    {
        None = 0,
        Required = 1,
        Optional = 2,
        Resource = 3
    }
}
