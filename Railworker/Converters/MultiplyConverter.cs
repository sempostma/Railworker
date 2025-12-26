using System;
using System.Windows.Data;

namespace Railworker.Converters
{
    [ValueConversion(typeof(double), typeof(double))]
    public class MultiplyConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null)
                return 0;

            double multiplier;
            double parameterValue;

            // Try to convert the value to double
            if (!double.TryParse(value.ToString(), out multiplier))
                return 0;

            // Try to convert the parameter to double
            if (!double.TryParse(parameter.ToString(), out parameterValue))
                return 0;

            return multiplier * parameterValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
