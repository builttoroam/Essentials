using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.ViewModel
{
    public class CalendarViewModel : BaseViewModel
    {
        public CalendarViewModel()
        {
            GetCalendars = new Command(OnClickGetCalendars);
            GetAllEvents = new Command(OnClickGetEvents);
            RequestCalendarReadAccess = new Command(OnRequestCalendarReadAccess);
            RequestCalendarWriteAccess = new Command(OnRequestCalendarWriteAccess);
        }

        public ICommand GetCalendars { get; }

        public ICommand GetAllEvents { get; }

        public ICommand RequestCalendarReadAccess { get; }

        public ICommand RequestCalendarWriteAccess { get; }

        public ObservableCollection<ICalendar> Calendars { get; } = new ObservableCollection<ICalendar>();

        public ObservableCollection<IEvent> Events { get; } = new ObservableCollection<IEvent>();

        async void OnClickGetCalendars()
        {
            Calendars.Clear();
            var calendars = await Calendar.GetCalendarsAsync();
            foreach (var calendar in calendars)
            {
                Calendars.Add(calendar);
            }
        }

        async void OnClickCalendarSpecificEvents(string calendarId)
        {
            Events.Clear();
            var events = await Calendar.GetEventsAsync(calendarId);
            foreach (var evnt in events)
            {
                Events.Add(evnt);
            }
        }

        async void OnClickGetEvents()
        {
            Events.Clear();
            var events = await Calendar.GetEventsAsync();
            foreach (var evnt in events)
            {
                Events.Add(evnt);
            }
        }

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
    }
}
