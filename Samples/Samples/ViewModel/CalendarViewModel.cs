using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.ViewModel
{
    public class CalendarViewModel : BaseViewModel
    {
        // public ObservableCollection<Calendar> calendars;

        public CalendarViewModel()
        {
            GetCalendars = new Command(OnClickGetCalendars);
            RequestCalendarReadAccess = new Command(OnRequestCalendarReadAccess);
            RequestCalendarWriteAccess = new Command(OnRequestCalendarWriteAccess);
        }

        public ICommand GetCalendars { get; }

        public ICommand RequestCalendarReadAccess { get; }

        public ICommand RequestCalendarWriteAccess { get; }

        public ObservableCollection<ICalendar> Calendars { get; } = new ObservableCollection<ICalendar>();

        async void OnClickGetCalendars()
        {
            Calendars.Clear();
            var calendars = await Calendar.GetCalendarsAsync();
            foreach (var calendar in calendars)
            {
                Calendars.Add(calendar);
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
                await DisplayAlertAsync($"Unable to request calendar read access: {ex.Message}");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync($"Unable to request calendar read access: {ex.Message}");
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
