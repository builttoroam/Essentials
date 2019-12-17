using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Samples.ViewModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.View
{
    public partial class CalendarEventPage : BasePage
    {
        DeviceEvent ViewModel => BindingContext as DeviceEvent;

        public CalendarEventPage()
        {
            InitializeComponent();
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            var vm = BindingContext as DeviceEvent;
            var evnt = await Calendar.GetEventByIdAsync(vm.Id);
            BindingContext = evnt;
        }

        async void OnDeleteEventButtonClicked(object sender, EventArgs e)
        {
            if (!(EventId.Text is string eventId) || string.IsNullOrEmpty(eventId.ToString()))
                return;

            var calendarEvent = await Calendar.GetEventByIdAsync(eventId.ToString());

            if (!(calendarEvent is DeviceEvent))
                return;

            await Calendar.DeleteCalendarEventById(eventId, CalendarId.Text);
            await DisplayAlert("Info", "Deleted event id: " + eventId, "Ok");
            await Navigation.PopAsync();
        }

        async void OnAddAttendeeButtonClicked(object sender, EventArgs e)
        {
            var modal = new CalendarEventAttendeeAddPage();

            if (!(EventId.Text is string eventId) || string.IsNullOrEmpty(eventId) || !(EventName.Text is string eventName) || string.IsNullOrEmpty(eventName))
                return;

            modal.BindingContext = new CalendarEventAddAttendeeViewModel(eventId, eventName);
            await Navigation.PushAsync(modal);
        }

        async void OnRemoveAttendeeFromEventButtonClicked(object sender, EventArgs e)
        {
            if (!(sender is Button btn) || !(EventId.Text is string eventId) || string.IsNullOrEmpty(eventId.ToString()))
                return;

            var attendee = btn?.BindingContext as DeviceEventAttendee;

            if (attendee is DeviceEventAttendee)
            {
                var success = await Calendar.RemoveAttendeeFromEvent(attendee, eventId);

                if (success)
                {
                    var lst = ViewModel.Attendees.ToList();
                    var attendeeToRemove = lst.Where(x => x.Email == attendee.Email && x.Name == attendee.Name).FirstOrDefault();
                    if (attendeeToRemove != null)
                    {
                        lst.Remove(attendeeToRemove);
                    }
                    BindingContext = new DeviceEvent()
                    {
                        AllDay = ViewModel.AllDay,
                        Attendees = lst,
                        CalendarId = ViewModel.CalendarId,
                        Description = ViewModel.Description,
                        Duration = ViewModel.Duration,
                        EndDate = ViewModel.EndDate,
                        Id = ViewModel.Id,
                        Location = ViewModel.Location,
                        StartDate = ViewModel.StartDate,
                        Title = ViewModel.Title
                    };
                }
            }
        }

        async void OnEditEventButtonClicked(object sender, EventArgs e)
        {
            var modal = new CalendarEventAddPage();

            modal.BindingContext = new CalendarEventAddViewModel(ViewModel.CalendarId, $"Edit: {ViewModel.Title}", ViewModel);
            await Navigation.PushAsync(modal);
        }
    }
}
