using System;
using System.Collections.ObjectModel;
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

        void OnEventTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item == null && e.Item is Event)
                return;

            var modal = new CalendarEventPage();
            modal.BindingContext = e.Item as Event;
            Navigation.PushModalAsync(modal);
        }
    }
}
