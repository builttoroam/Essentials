using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using DateTimeOffset = System.DateTimeOffset;

namespace Samples.ViewModel
{
    public class CalendarViewModel : BaseViewModel
    {
        public CalendarViewModel()
        {
            GetCalendars = new Command(OnClickGetCalendars);
            RequestCalendarReadAccess = new Command(OnRequestCalendarReadAccess);
            RequestCalendarWriteAccess = new Command(OnRequestCalendarWriteAccess);
            StartDateSelectedCommand = new Command(OnStartDateSelected);
            StartTimeSelectedCommand = new Command(OnStartTimeSelected);
            EndDateSelectedCommand = new Command(OnEndDateSelected);
            EndTimeSelectedCommand = new Command(OnEndTimeSelected);
        }

        ICalendar selectedCalendar;

        public ICommand GetCalendars { get; }

        public ICommand RequestCalendarReadAccess { get; }

        public ICommand RequestCalendarWriteAccess { get; }

        public ICommand StartDateSelectedCommand { get; }

        public ICommand StartTimeSelectedCommand { get; }

        public ICommand EndDateSelectedCommand { get; }

        public ICommand EndTimeSelectedCommand { get; }

        public bool HasCalendarReadAccess { get; set; }

        public DateTime? StartDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public DateTime? EndDate { get; set; }

        public TimeSpan? EndTime { get; set; }

        public ObservableCollection<ICalendar> Calendars { get; } = new ObservableCollection<ICalendar>();

        public ObservableCollection<IEvent> Events { get; } = new ObservableCollection<IEvent>();

        public ICalendar SelectedCalendar
        {
            get => selectedCalendar;

            set
            {
                if (SetProperty(ref selectedCalendar, value) && selectedCalendar != null)
                {
                    var endDate = EndDate.HasValue ? EndDate += EndTime.GetValueOrDefault() : null;
                    var startDate = StartDate.HasValue ? StartDate += StartTime.GetValueOrDefault() : null;
                    OnChangeRequestCalendarSpecificEvents(selectedCalendar.Id, startDate, endDate);
                }
            }
        }

        public override void OnAppearing()
        {
            base.OnAppearing();
        }

        async void OnClickGetCalendars()
        {
            Calendars.Clear();
            Calendars.Add(new DeviceCalendar() { Id = null, IsReadOnly = true, Name = "All" });
            var calendars = await Calendar.GetCalendarsAsync();
            foreach (var calendar in calendars)
            {
                Calendars.Add(calendar);
            }
            SelectedCalendar = Calendars[0];
        }

        void OnStartDateSelected(object parameter)
        {
            var startDate = parameter as DateTime?;

            if (!startDate.HasValue)
                return;

            startDate += StartTime.GetValueOrDefault();
            var endDate = EndDate.HasValue ? EndDate += EndTime.GetValueOrDefault() : null;

            RefreshEventList(SelectedCalendar?.Id, startDate, endDate);
        }

        void OnStartTimeSelected(object parameter)
        {
            var endDate = EndDate.HasValue ? EndDate += EndTime.GetValueOrDefault() : null;
            var startDate = StartDate.HasValue ? StartDate += StartTime.GetValueOrDefault() : null;

            RefreshEventList(SelectedCalendar?.Id, startDate, endDate);
        }

        void OnEndDateSelected(object parameter)
        {
            var endDate = parameter as DateTime?;

            if (!endDate.HasValue)
                return;

            endDate += EndTime.GetValueOrDefault();
            var startDate = StartDate.HasValue ? StartDate += StartTime.GetValueOrDefault() : null;

            RefreshEventList(SelectedCalendar?.Id, startDate, endDate);
        }

        void OnEndTimeSelected(object parameter)
        {
            var endDate = EndDate.HasValue ? EndDate += EndTime.GetValueOrDefault() : null;
            var startDate = StartDate.HasValue ? StartDate += StartTime.GetValueOrDefault() : null;

            RefreshEventList(SelectedCalendar?.Id, startDate, endDate);
        }

        void OnChangeRequestCalendarSpecificEvents(string calendarId = null, DateTime? startDateTime = null, DateTime? endDateTime = null) => RefreshEventList(calendarId, startDateTime, endDateTime);

        async void OnRequestCalendarWriteAccess()
        {
            try
            {
                await Calendar.RequestCalendarWriteAccess();
            }
            catch (PermissionException ex)
            {
                await DisplayAlertAsync($"Unable to request calendar write access: {ex.Message}");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync($"Unable to request calendar write access: {ex.Message}");
            }
        }

        async void OnRequestCalendarReadAccess()
        {
            try
            {
                await Calendar.RequestCalendarReadAccess();
            }
            catch (PermissionException ex)
            {
                await DisplayAlertAsync($"Unable to request calendar read access: {ex.Message}");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync($"Unable to request calendar read access: {ex.Message}");
            }
        }

        async void RefreshEventList(string calendarId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            Events.Clear();
            var events = await Calendar.GetEventsAsync(SelectedCalendar?.Id, startDate?.ToUniversalTime(), endDate?.ToUniversalTime());
            foreach (var evnt in events)
            {
                Events.Add(evnt);
            }
        }
    }
}
