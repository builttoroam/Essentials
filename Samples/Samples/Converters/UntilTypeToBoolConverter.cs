using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Samples.ViewModel;
using Xamarin.Forms;

namespace Samples.Converters
{
    public class UntilTypeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || !(value is string type))
            {
                return false;
            }
            switch (type)
            {
                case RecurrenceEndType.Indefinitely:
                    return false;
                case RecurrenceEndType.AfterOccurences:
                    return false;
                case RecurrenceEndType.UntilEndDate:
                    return true;
                default:
                    return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
