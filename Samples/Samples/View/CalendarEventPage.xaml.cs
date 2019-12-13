using System;
using System.Collections.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.View
{
    public partial class CalendarEventPage : BasePage
    {
        public CalendarEventPage()
        {
            InitializeComponent();
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
    }
}
