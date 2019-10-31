using System.Collections.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.View
{
    public partial class CalendarPage : TabbedPage
    {
        public CalendarPage()
        {
            InitializeComponent();
        }

        public async void OnClickCalendarSpecificEvents(object sender, ItemTappedEventArgs e)
        {
            if (e != null && e.Item is DeviceCalendar)
            {
                var eventListView = new ObservableCollection<IEvent>();
                var events = await Calendar.GetEventsAsync((e.Item as ICalendar).Id);
                foreach (var evnt in events)
                {
                    eventListView.Add(evnt);
                }
                var listView = new ListView
                {
                    ItemsSource = eventListView,
                    ItemTemplate = new DataTemplate(() =>
                    {
                        var title = new Label();
                        title.SetBinding(Label.TextProperty, "Title");

                        return new ViewCell
                        {
                            View = new StackLayout
                            {
                                Children =
                                {
                                    title
                                }
                            }
                        };
                    })
                };
                var detailPage = new ContentPage
                {
                    BindingContext = e.Item as ICalendar,
                    Content = new StackLayout()
                    {
                        Children = { listView }
                    }
                };
                CalendarList.SelectedItem = null;
                await Navigation.PushModalAsync(detailPage);
            }
        }
    }
}
