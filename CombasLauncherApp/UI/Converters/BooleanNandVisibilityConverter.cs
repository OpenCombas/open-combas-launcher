using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CombasLauncherApp.UI.Converters
{
    public class BooleanNandVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[]? values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
            {
                return Visibility.Visible;
            }

            var allTrue = values.All(v => v is true);
            return allTrue ? Visibility.Collapsed : Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}