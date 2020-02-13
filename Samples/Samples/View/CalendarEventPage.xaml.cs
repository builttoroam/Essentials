using System;
using System.Linq;
using Samples.ViewModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.View
{
    public partial class CalendarEventPage : BasePage
    {
        const string actionResponseDeleteAll = "Yes to All";
        const string actionResponseDeleteOne = "Just this one";
        const string actionResponseDeleteForward = "From this date forward";
        const string actionResponseOk = "Ok";
        const string actionResponseYes = "Yes";
        const string actionResponseCancel = "Cancel";
        const string actionTitleInfo = "Info";
        const string actionTitleWarning = "Warning!";

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

            var answer = await DisplayAlert(actionTitleWarning, $"Are you sure you want to delete {calendarEvent.Title}? (this cannot be undone)", actionResponseYes, actionResponseCancel);
            if (answer)
            {
                if (calendarEvent.RecurrancePattern != null)
                {
                    var action = await DisplayActionSheet("Do you want to delete all instances of this event?", actionResponseCancel, null, actionResponseDeleteAll, actionResponseDeleteOne, actionResponseDeleteForward);
                    var deletionConfirmed = false;
                    var deletionMessage = string.Empty;
                    switch (action)
                    {
                        case actionResponseDeleteAll:
                            deletionConfirmed = await Calendars.DeleteCalendarEventById(eventId, CalendarId.Text);
                            deletionMessage = $"Deleted event id: {eventId}";
                            break;
                        case actionResponseDeleteOne:
                            deletionConfirmed = await Calendars.DeleteCalendarEventInstanceByDate(eventId, CalendarId.Text, calendarEvent.StartDate);
                            deletionMessage = $"Deleted instance of event id: {eventId}";
                            break;
                        case actionResponseDeleteForward:
                            deletionConfirmed = await Calendars.SetEventRecurrenceEndDate(eventId, calendarEvent.StartDate.AddDays(-1));
                            deletionMessage = $"Deleted all future instances of event id: {eventId}";
                            break;
                    }

                    if (deletionConfirmed)
                    {
                        await DisplayAlert(actionTitleInfo, deletionMessage, actionResponseOk);
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert(actionTitleInfo, "Unable to delete event: " + eventId, actionResponseOk);
                    }
                }
                else if (await Calendars.DeleteCalendarEventById(eventId, CalendarId.Text))
                {
                    await DisplayAlert(actionTitleInfo, "Deleted event id: " + eventId, actionResponseOk);
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
