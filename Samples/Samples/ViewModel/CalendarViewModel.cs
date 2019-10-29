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
            GetCalendars = new Command(OnClick);
            RequestCalendarReadAccess = new Command(OnRequestCalendarReadAccess);
            RequestCalendarWriteAccess = new Command(OnRequestCalendarWriteAccess);
        }

        public ICommand GetCalendars { get; }

        public ICommand RequestCalendarReadAccess { get; }

        public ICommand RequestCalendarWriteAccess { get; }

        public string Results { get; set; }

        async void OnClick()
        {
            var listResults = await Calendar.GetCalendarsAsync();
            Results = listResults.First().Name;
            OnPropertyChanged(nameof(Results));
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
