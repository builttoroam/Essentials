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

        async void OnEventTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item == null || !(e.Item is Event evt))
                return;

            var evnt = await Calendar.GetEventByIdAsync(evt.Id);
            var modal = new CalendarEventPage { BindingContext = evnt };
            await Navigation.PushAsync(modal);
        }
    }
}
