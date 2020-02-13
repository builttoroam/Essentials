using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.ViewModel
{
    public class CalendarEventViewModel : BaseViewModel
    {
        public CalendarEventViewModel(CalendarEvent calendarEvent)
        {
            RemoveAttendeeClickedCommand = new Command(OnRemoveAttendeeClicked);
            RemoveReminderClickedCommand = new Command(OnRemoveReminderClicked);
            AddReminderClickedCommand = new Command(OnAddReminderClicked);
            Id = calendarEvent.Id;
            CalendarId = calendarEvent.CalendarId;
            Title = calendarEvent.Title;
            Description = calendarEvent.Description;
            Location = calendarEvent.Location;
            StartDate = calendarEvent.StartDate;
            Url = calendarEvent.Url;
            EndDate = calendarEvent.EndDate;
            Attendees = calendarEvent.Attendees != null ? new ObservableCollection<CalendarEventAttendee>(calendarEvent.Attendees) : null;
            Reminder = calendarEvent.Reminder;
            ReminderMinutes = calendarEvent.Reminder != null ? calendarEvent.Reminder.MinutesPriorToEventStart : 0;
            RecurrancePattern = calendarEvent.RecurrancePattern;
        }

        public ICommand AddReminderClickedCommand { get; }

        public bool AllDay
        {
            get => !EndDate.HasValue;
            set => EndDate = value ? (DateTimeOffset?)null : StartDate;
        }

        public async void RefreshAttendees()
        {
            Attendees.Clear();

            var attendees = (await Calendars.GetEventByIdAsync(Id)).Attendees;
            foreach (var attendee in attendees)
            {
                Attendees.Add(attendee);
            }
        }

        public ObservableCollection<CalendarEventAttendee> Attendees { get; set; }

        public string CalendarId { get; set; }

        public string Description { get; set; }

        public TimeSpan? Duration
        {
            get => EndDate.HasValue ? EndDate - StartDate : null;
            set => EndDate = value.HasValue ? StartDate.Add(value.Value) : (DateTimeOffset?)null;
        }

        public DateTimeOffset? EndDate { get; set; }

        public bool HasReminder => Reminder != null;

        public string Id { get; set; }

        public string Location { get; set; }

        public RecurrenceRule RecurrancePattern { get; set; }

        public CalendarEventReminder Reminder { get; set; }

        public int ReminderMinutes { get; set; }

        public ICommand RemoveAttendeeClickedCommand { get; }

        public ICommand RemoveReminderClickedCommand { get; }

        public DateTimeOffset StartDate { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        async void OnAddReminderClicked(object parameter)
        {
            if (await Calendars.AddReminderToEvent(new CalendarEventReminder() { MinutesPriorToEventStart = Math.Abs(ReminderMinutes) }, Id))
            {
                Reminder = new CalendarEventReminder() { MinutesPriorToEventStart = Math.Abs(ReminderMinutes) };
                OnPropertyChanged(nameof(HasReminder));
                OnPropertyChanged(nameof(ReminderMinutes));
            }
        }

        async void OnRemoveAttendeeClicked(object parameter)
        {
            if (parameter == null || !(parameter is CalendarEventAttendee attendee))
            {
                return;
            }

            if (await Calendars.RemoveAttendeeFromEvent(attendee, Id))
            {
                var attendeeToRemove = Attendees.FirstOrDefault(x => x.Email == attendee.Email && x.Name == attendee.Name);
                if (attendeeToRemove != null)
                {
                    Attendees.Remove(attendeeToRemove);
                }
            }
        }

        async void OnRemoveReminderClicked(object parameter)
        {
            if (await Calendars.RemoveReminderFromEvent(Id))
            {
                Reminder = null;
                ReminderMinutes = 0;
                OnPropertyChanged(nameof(HasReminder));
                OnPropertyChanged(nameof(ReminderMinutes));
            }
        }
    }
}
