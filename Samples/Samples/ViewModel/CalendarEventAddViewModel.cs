﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.ViewModel
{
    public class CalendarEventAddViewModel : BaseViewModel
    {
        public CalendarEventAddViewModel(string calendarId, string calendarName)
        {
            CalendarId = calendarId;
            CalendarName = calendarName;
            CreateEvent = new Command(CreateCalendarEvent);
        }

        public string CalendarId { get; set; }

        public string CalendarName { get; set; }

        public string EventTitle { get; set; }

        public string Description { get; set; }

        public string EventLocation { get; set; }

        public bool AllDay { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Now;

        public TimeSpan StartTime { get; set; }

        public DateTime EndDate { get; set; } = DateTime.Now;

        public TimeSpan EndTime { get; set; }

        public ICommand CreateEvent { get; }

        async void CreateCalendarEvent()
        {
            await Calendar.RequestCalendarWriteAccess();
            var startDto = new DateTimeOffset(StartDate + StartTime);
            var endDto = new DateTimeOffset(EndDate + EndTime);
            var newEvent = new Event()
            {
                CalendarId = CalendarId,
                Title = EventTitle,
                AllDay = AllDay,
                Description = Description,
                Start = startDto.ToUnixTimeMilliseconds(),
                End = endDto.ToUnixTimeMilliseconds()
            };

            var eventId = await Calendar.CreateCalendarEvent(newEvent);
        }
    }
}
