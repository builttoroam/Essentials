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
            if (e.Item == null)
                return;

            var modal = new CalendarEventPage();
            modal.BindingContext = e.Item as Event;
            Navigation.PushModalAsync(modal);
        }

        void OnAddEventButtonClicked(object sender, EventArgs e)
        {
            var modal = new CalendarEventAddPage();

            if (!(SelectedCalendar.SelectedItem is ICalendar tst) || string.IsNullOrEmpty(tst.Id))
                return;

            modal.BindingContext = new CalendarEventAddViewModel(tst.Id, tst.Name);
            Navigation.PushModalAsync(modal);
        }
    }
}
