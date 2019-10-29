using System;
using System.Collections.Generic;
using System.Text;
using EventKit;

namespace Xamarin.Essentials
{
    static class CalendarRequest
    {
        static readonly Lazy<EKEventStore> EventStore = new Lazy<EKEventStore>(() => new EKEventStore());

        public static EKEventStore Instance => EventStore.Value;
    }
}
