using System;
using System.Collections.Generic;

namespace Xamarin.Essentials
{
    public interface ICalendar
    {
        string Id { get; }

        string Name { get; }

        bool IsReadOnly { get; }

        // Android specific function, as android has a period of time where calendars are 'soft-deleted'
        bool Deleted { get; }
    }

    [Preserve(AllMembers = true)]
    public class DeviceCalendar : ICalendar
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public bool IsReadOnly { get; set; }

        public bool Deleted { get; set; }
    }

    public interface IEvent
    {
        string Id { get; }

        string CalendarId { get; }

        string Title { get; }

        string Description { get; }

        string Location { get; }

        bool AllDay { get; }

        long? Start { get; }

        long? End { get; }

        IReadOnlyList<IAttendee> Attendees { get; }

        // Android specific function, as android has a period of time where calendar events are 'soft-deleted'
        bool Deleted { get; }
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

        public IReadOnlyList<IAttendee> Attendees { get; set; }

        public bool Deleted { get; set; }
    }

    public interface IAttendee
    {
        string Name { get; }

        string Email { get; }
    }

    [Preserve(AllMembers = true)]
    public class Attendee : IAttendee
    {
        public string Name { get; set; }

        public string Email { get; set; }
    }
}
