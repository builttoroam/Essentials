using System;
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
                    if (await DisplayAlert("Warning!", $"Do you want to delete all instances of this event?", "Yes All", "Just this one"))
                    {
                        if (await Calendars.DeleteCalendarEventById(eventId, CalendarId.Text))
                        {
                            await DisplayAlert("Info", "Deleted event id: " + eventId, "Ok");
                            await Navigation.PopAsync();
                        }
                    }
                    else if (await Calendars.DeleteCalendarEventInstanceByDate(eventId, CalendarId.Text, calendarEvent.StartDate))
                    {
                        await DisplayAlert("Info", "Deleted event id: " + eventId, "Ok");
                        await Navigation.PopAsync();
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
                    ViewModel.Attendees = lst;
                    RefreshValues(ViewModel);
                }
            }
        }

        async void OnRemoveReminderFromEventButtonClicked(object sender, EventArgs e)
        {
            if (!(sender is Button btn) || !(EventId.Text is string eventId) || string.IsNullOrEmpty(eventId))
                return;

            if (await Calendars.RemoveReminderFromEvent(eventId))
            {
                ViewModel.Reminder = null;
                RefreshValues(ViewModel);
            }
        }

        async void OnEditEventButtonClicked(object sender, EventArgs e)
        {
            var modal = new CalendarEventAddPage();

            var calendarName = (await Calendars.GetCalendarsAsync()).FirstOrDefault(x => x.Id == ViewModel.CalendarId)?.Name;

            modal.BindingContext = new CalendarEventAddViewModel(ViewModel.CalendarId, calendarName, ViewModel);
            await Navigation.PushAsync(modal);
        }

        async void OnAddReminderFromEventButtonClicked(object sender, EventArgs e)
        {
            if (!(EventId.Text is string eventId) || string.IsNullOrEmpty(eventId) || !(Reminder.Text is string reminderMinutesString) || !int.TryParse(reminderMinutesString, out var reminderMinutes))
                return;

            if (await Calendars.AddReminderToEvent(new CalendarEventReminder() { MinutesPriorToEventStart = Math.Abs(reminderMinutes) }, eventId))
            {
                ViewModel.Reminder = new CalendarEventReminder() { MinutesPriorToEventStart = Math.Abs(reminderMinutes) };
                RefreshValues(ViewModel);
            }
        }

        void RefreshValues(CalendarEvent eventRefresh) =>
            BindingContext = new CalendarEvent()
            {
                AllDay = eventRefresh.AllDay,
                Attendees = eventRefresh.Attendees,
                CalendarId = eventRefresh.CalendarId,
                Description = eventRefresh.Description,
                Duration = eventRefresh.Duration,
                EndDate = eventRefresh.EndDate,
                RecurrancePattern = eventRefresh.RecurrancePattern,
                Id = eventRefresh.Id,
                Location = eventRefresh.Location,
                StartDate = eventRefresh.StartDate,
                Title = eventRefresh.Title,
                Reminder = eventRefresh.Reminder,
                Url = eventRefresh.Url
            };
    }
}
