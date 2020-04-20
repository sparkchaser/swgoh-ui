using System;
using System.Globalization;
using System.Windows.Data;

namespace goh_ui
{
    /// <summary>
    /// Re-format a number with commas separating the digits.
    /// </summary>
    public class FormattedNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return $"{value:N0}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
