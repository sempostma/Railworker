using System;
using System.Globalization;
using System.Windows.Data;

namespace Railworker.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    public class VehicleImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "/LocoSwap;component/Resources/PreviewNotAvailable.png";
            Vehicle vehicle = (Vehicle)value;
            //return VehicleAvailibility.GetVehicleImage(vehicle);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
