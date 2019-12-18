using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace Samples.Converters
{
    public class AttendeeRequiredColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool required))
            {
                return Color.PaleVioletRed;
            }

            if (required)
            {
                return Color.LightGoldenrodYellow;
            }
            return Color.LightGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
