using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Samples.ViewModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.View
{
    public partial class CalendarPage : BasePage
    {
        public CalendarPage()
        {
            InitializeComponent();
        }

        async void OnAddEventButtonClicked(object sender, EventArgs e)
        {
            var modal = new CalendarEventAddPage();

            if (!(SelectedCalendar.SelectedItem is DeviceCalendar tst) || string.IsNullOrEmpty(tst.Id))
                return;

            modal.BindingContext = new CalendarEventAddViewModel(tst.Id, tst.Name);
            await Navigation.PushAsync(modal);
        }

        async void OnEventTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item == null || !(e.Item is DeviceEvent evt))
                return;

            var calendarEvent = await Calendar.GetEventByIdAsync((e.Item as DeviceEvent)?.Id);
            var modal = new CalendarEventPage
            {
                BindingContext = calendarEvent
            };
            await Navigation.PushAsync(modal);
        }
    }
}
