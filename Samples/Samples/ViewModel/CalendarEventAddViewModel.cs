using System;
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

        string eventTitle;

        public string EventTitle
        {
            get => eventTitle;
            set
            {
                if (SetProperty(ref eventTitle, value))
                {
                    OnPropertyChanged(nameof(CanCreateEvent));
                }
            }
        }

        public bool CanCreateEvent => !string.IsNullOrWhiteSpace(EventTitle) && !string.IsNullOrWhiteSpace(Description) && !string.IsNullOrWhiteSpace(EventLocation) && ((EndDate.Date == StartDate.Date && (EndTime > StartTime || AllDay)) || EndDate.Date > StartDate.Date);

        string description;

        public string Description
        {
            get => description;
            set
            {
                if (SetProperty(ref description, value))
                {
                    OnPropertyChanged(nameof(CanCreateEvent));
                }
            }
        }

        string eventLocation;

        public string EventLocation
        {
            get => eventLocation;
            set
            {
                if (SetProperty(ref eventLocation, value))
                {
                    OnPropertyChanged(nameof(CanCreateEvent));
                }
            }
        }

        bool allDay;

        public bool AllDay
        {
            get => allDay;
            set
            {
                if (SetProperty(ref allDay, value))
                {
                    OnPropertyChanged(nameof(CanCreateEvent));
                }
            }
        }

        DateTime startDate = DateTime.Now.Date;

        public DateTime StartDate
        {
            get => startDate;
            set
            {
                if (SetProperty(ref startDate, value))
                {
                    OnPropertyChanged(nameof(CanCreateEvent));
                }
            }
        }

        TimeSpan startTime = DateTime.Now.AddHours(1).TimeOfDay.RoundToNearestMinutes(30);

        public TimeSpan StartTime
        {
            get => startTime;
            set
            {
                if (SetProperty(ref startTime, value))
                {
                    OnPropertyChanged(nameof(CanCreateEvent));
                }
            }
        }

        DateTime endDate = DateTime.Now.Date;

        public DateTime EndDate
        {
            get => endDate;
            set
            {
                if (SetProperty(ref endDate, value))
                {
                    OnPropertyChanged(nameof(CanCreateEvent));
                }
            }
        }

        TimeSpan endTime = DateTime.Now.AddHours(2).TimeOfDay.RoundToNearestMinutes(30);

        public TimeSpan EndTime
        {
            get => endTime;
            set
            {
                if (SetProperty(ref endTime, value))
                {
                    OnPropertyChanged(nameof(CanCreateEvent));
                }
            }
        }

        public ICommand CreateEvent { get; }

        async void CreateCalendarEvent()
        {
            var startDto = new DateTimeOffset(StartDate + StartTime);
            var endDto = new DateTimeOffset(EndDate + EndTime);
            var newEvent = new DeviceEvent()
            {
                CalendarId = CalendarId,
                Title = EventTitle,
                AllDay = AllDay,
                Description = Description,
                Location = EventLocation,
                StartDate = startDto,
                EndDate = endDto
            };

            var eventId = await Calendar.CreateCalendarEvent(newEvent);

            await DisplayAlertAsync("Created event id: " + eventId);
        }
    }
}
