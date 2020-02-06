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
        CalendarEvent ViewModel => BindingContext as CalendarEvent;

        public CalendarEventPage()
        {
            InitializeComponent();
        }

        async void OnDeleteEventButtonClicked(object sender, EventArgs e)
        {
            if (!(EventId.Text is string eventId) || string.IsNullOrEmpty(eventId))
                return;

            var viewModel = BindingContext as CalendarEvent;

            var calendarEvent = await Calendars.GetEventInstanceByIdAsync(eventId, viewModel.StartDate);

            if (!(calendarEvent is CalendarEvent))
                return;

            var answer = await DisplayAlert("Warning!", $"Are you sure you want to delete {calendarEvent.Title}? (this cannot be undone)", "Yes", "Cancel");
            if (answer)
            {
                if (calendarEvent.RecurrancePattern != null)
                {
                    var action = await DisplayActionSheet("Do you want to delete all instances of this event?", "Cancel", null, "Yes to All", "Just this one", "From this date forward");

                    if (action == "Yes to All")
                    {
                        if (await Calendars.DeleteCalendarEventById(eventId, CalendarId.Text))
                        {
                            await DisplayAlert("Info", "Deleted event id: " + eventId, "Ok");
                            await Navigation.PopAsync();
                        }
                    }
                    else if (action == "Just this one")
                    {
                        if (await Calendars.DeleteCalendarEventInstanceByDate(eventId, CalendarId.Text, calendarEvent.StartDate))
                        {
                            await DisplayAlert("Info", "Deleted instance of event id: " + eventId, "Ok");
                            await Navigation.PopAsync();
                        }
                    }
                    else if (action == "From this date forward")
                    {
                        if (await Calendars.SetEventRecurrenceEndDate(eventId, calendarEvent.StartDate.AddDays(-1)))
                        {
                            await DisplayAlert("Info", "Deleted all future instances of event id: " + eventId, "Ok");
                            await Navigation.PopAsync();
                        }
                    }
                }
                else if (await Calendars.DeleteCalendarEventById(eventId, CalendarId.Text))
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
            if (!(sender is Button btn) || !(EventId.Text is string eventId) || string.IsNullOrEmpty(eventId))
                return;

            var attendee = btn?.BindingContext as CalendarEventAttendee;

            if (attendee is CalendarEventAttendee)
            {
                var success = await Calendars.RemoveAttendeeFromEvent(attendee, eventId);

                if (success)
                {
                    var lst = ViewModel.Attendees.ToList();
                    var attendeeToRemove = lst.FirstOrDefault(x => x.Email == attendee.Email && x.Name == attendee.Name);
                    if (attendeeToRemove != null)
                    {
                        lst.Remove(attendeeToRemove);
                    }
                    BindingContext = new CalendarEvent()
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
            if (!(sender is Button btn) || !(EventId.Text is string eventId) || string.IsNullOrEmpty(eventId))
                return;

            var attendee = btn?.BindingContext as CalendarEventReminder;
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
