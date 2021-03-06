﻿using System;
using System.Collections.Generic;

namespace Xamarin.Essentials
{
    [Preserve(AllMembers = true)]
    public class Calendar
    {
        public string Id { get; set; }

        public string Name { get; set; }
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

        public IEnumerable<CalendarEventAttendee> Attendees { get; set; }
    }

    [Preserve(AllMembers = true)]
    public class CalendarEventAttendee
    {
        public string Name { get; set; }

        public string Email { get; set; }
    }
}
