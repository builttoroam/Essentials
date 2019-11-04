using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace Samples.Converters
{
    public class EpochToDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => DateTimeOffset.FromUnixTimeMilliseconds((long)value).ToString("dd/MM/yy hh:mm tt");

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => DateTimeOffset.TryParse((string)value, out var dto) ? dto.ToUnixTimeMilliseconds() : 0;
    }
}
