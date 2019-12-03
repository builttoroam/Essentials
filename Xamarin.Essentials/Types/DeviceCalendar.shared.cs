﻿using System;
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

        public bool AllDay { get; set; }

        public DateTimeOffset? StartDate { get; set; }

        public TimeSpan Duration { get; set; }

        public DateTimeOffset? EndDate { get; set; }

        public IEnumerable<DeviceEventAttendee> Attendees { get; set; }
    }

    [Preserve(AllMembers = true)]
    public class DeviceEventAttendee
    {
        public string Name { get; set; }

        public string Email { get; set; }
    }
}
