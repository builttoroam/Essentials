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

        // protected override async void OnAppearing()
        // {
        //     base.OnAppearing();
        //     var vm = BindingContext as DeviceEvent;
        //     var evnt = await Calendar.GetEventInstanceByIdAsync(vm.Id, vm.StartDate);
        //     BindingContext = evnt;
        // }

        async void OnDeleteEventButtonClicked(object sender, EventArgs e)
        {
            if (!(EventId.Text is string eventId) || string.IsNullOrEmpty(eventId.ToString()))
                return;

            var vm = BindingContext as DeviceEvent;

            var calendarEvent = await Calendar.GetEventInstanceByIdAsync(eventId.ToString(), vm.StartDate);

            if (!(calendarEvent is DeviceEvent))
                return;

            var answer = await DisplayAlert("Warning!", $"Are you sure you want to delete {calendarEvent.Title}? (this cannot be undone)", "Yes", "Cancel");
            if (answer)
            {
                if (calendarEvent.RecurrancePattern != null)
                {
                    if (await DisplayAlert("Warning!", $"Do you want to delete all instances of this event?", "Yes All", "Just this one"))
                    {
                        if (await Calendar.DeleteCalendarEventById(eventId, CalendarId.Text))
                        {
                            await DisplayAlert("Info", "Deleted event id: " + eventId, "Ok");
                            await Navigation.PopAsync();
                        }
                    }
                    else if (await Calendar.DeleteCalendarEventInstanceByDate(eventId, CalendarId.Text, calendarEvent.StartDate))
                    {
                        await DisplayAlert("Info", "Deleted event id: " + eventId, "Ok");
                        await Navigation.PopAsync();
                    }
                }
                else if (await Calendar.DeleteCalendarEventById(eventId, CalendarId.Text))
                {
                    await DisplayAlert("Info", "Deleted event id: " + eventId, "Ok");
                    await Navigation.PopAsync();
                }
            }
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
                    var attendeeToRemove = lst.FirstOrDefault(x => x.Email == attendee.Email && x.Name == attendee.Name);
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

        void OnRemoveReminderFromEventButtonClicked(object sender, EventArgs e)
        {
            if (!(sender is Button btn) || !(EventId.Text is string eventId) || string.IsNullOrEmpty(eventId.ToString()))
                return;

            var attendee = btn?.BindingContext as DeviceEventReminder;
        }

        async void OnEditEventButtonClicked(object sender, EventArgs e)
        {
            var modal = new CalendarEventAddPage();

            var calendarName = (await Calendar.GetCalendarsAsync()).FirstOrDefault(x => x.Id == ViewModel.CalendarId)?.Name;

            modal.BindingContext = new CalendarEventAddViewModel(ViewModel.CalendarId, calendarName, ViewModel);
            await Navigation.PushAsync(modal);
        }
    }
}
