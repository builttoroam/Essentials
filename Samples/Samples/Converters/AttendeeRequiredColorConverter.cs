using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.Converters
{
    public class AttendeeRequiredColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is AttendeeType attendeeType))
            {
                return Color.PaleVioletRed;
            }

            if (attendeeType == AttendeeType.Required)
            {
                return Color.LightGoldenrodYellow;
            }
            else if (attendeeType == AttendeeType.Resource)
            {
                return Color.PaleGreen;
            }
            return Color.LightGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
