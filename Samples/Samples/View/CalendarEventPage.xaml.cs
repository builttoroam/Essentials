using System;
using System.Linq;
using Samples.ViewModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.View
{
    public partial class CalendarEventPage : BasePage
    {
        CalendarEventViewModel ViewModel => BindingContext as CalendarEventViewModel;

        public CalendarEventPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel?.RefreshAttendees();
        }

        async void OnDeleteEventButtonClicked(object sender, EventArgs e)
        {
            var calendarEvent = await Calendars.GetEventInstanceByIdAsync(ViewModel.Id, ViewModel.StartDate);

            if (!(calendarEvent is CalendarEvent))
                return;

            var answer = await DisplayAlert("Warning!", $"Are you sure you want to delete {calendarEvent.Title}? (this cannot be undone)", "Yes", "Cancel");
            if (answer)
            {
                if (calendarEvent.RecurrancePattern != null)
                {
                    if (await DisplayAlert("Warning!", $"Do you want to delete all instances of this event?", "Yes All", "Just this one"))
                    {
                        if (await Calendars.DeleteCalendarEventById(ViewModel.Id, CalendarId.Text))
                        {
                            await DisplayAlert("Info", "Deleted event id: " + ViewModel.Id, "Ok");
                            await Navigation.PopAsync();
                        }
                    }
                    else if (await Calendars.DeleteCalendarEventInstanceByDate(ViewModel.Id, CalendarId.Text, calendarEvent.StartDate))
                    {
                        await DisplayAlert("Info", "Deleted event id: " + ViewModel.Id, "Ok");
                        await Navigation.PopAsync();
                    }
                }
                else if (await Calendars.DeleteCalendarEventById(ViewModel.Id, CalendarId.Text))
                {
                    await DisplayAlert("Info", "Deleted event id: " + ViewModel.Id, "Ok");
                    await Navigation.PopAsync();
                }
            }
        }

        async void OnAddAttendeeButtonClicked(object sender, EventArgs e)
        {
            var modal = new CalendarEventAttendeeAddPage();

            modal.BindingContext = new CalendarEventAddAttendeeViewModel(ViewModel.Id, ViewModel.Title);
            await Navigation.PushAsync(modal);
        }

        async void OnEditEventButtonClicked(object sender, EventArgs e)
        {
            var modal = new CalendarEventAddPage();

            var calendarName = (await Calendars.GetCalendarsAsync()).FirstOrDefault(x => x.Id == ViewModel.CalendarId)?.Name;

            modal.BindingContext = new CalendarEventAddViewModel(ViewModel.CalendarId, calendarName, ViewModel);
            await Navigation.PushAsync(modal);
        }
    }
}
