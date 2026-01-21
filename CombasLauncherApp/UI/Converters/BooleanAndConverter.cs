using System.Globalization;
using System.Windows.Data;

namespace CombasLauncherApp.UI.Converters
{
    public class BooleanAndConverter : IMultiValueConverter
    {
        public object Convert(object[]? values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
            {
                return false;
            }

            return values.All(v => v is bool and true);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}