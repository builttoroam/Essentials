using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Samples.ViewModel;
using Xamarin.Forms;

namespace Samples.Converters
{
    public class ListSelectorArgsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var eventArgs = value as ObservableCollection<DayOfTheWeekSwitch>;

            if (eventArgs == null)
                return value;

            return eventArgs;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
