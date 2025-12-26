using System;
using System.Globalization;
using System.Windows.Data;
using static Railworker.Blueprint;

namespace LocoSwap.Converters
{
    [ValueConversion(typeof(BlueprintExistance), typeof(string))]
    public class VehicleStatusToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            var status = (BlueprintExistance)value;
            string image;
            switch (status)
            {
                case BlueprintExistance.Found:
                    image = "BulletGreen.png";
                    break;
                case BlueprintExistance.Replaced:
                    image = "Replaced.png";
                    break;
                case BlueprintExistance.Missing:
                default:
                    image = "BulletRed.png";
                    break;
            }
            string uri = String.Format("/LocoSwap;component/Resources/{0}", image);
            return uri;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


    [ValueConversion(typeof(BlueprintExistance), typeof(string))]
    public class ConsistStatusToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            //var status = (ConsistVehicleExistance)value;
            var status = false;
            string image;
            switch (status)
            {
                //case ConsistVehicleExistance.Found:
                //    image = "BulletGreen.png";
                //    break;
                //case ConsistVehicleExistance.FullyReplaced:
                //    image = "ReplacedGreen.png";
                //    break;
                //case ConsistVehicleExistance.PartiallyReplaced:
                //    image = "ReplacedRed.png";
                //    break;
                //case ConsistVehicleExistance.Missing:
                default:
                    image = "BulletRed.png";
                    break;
            }
            string uri = String.Format("/LocoSwap;component/Resources/{0}", image);
            return uri;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(BlueprintExistance), typeof(string))]
    public class ScenarioStatusToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            var status = (BlueprintExistance)value;
            string image;
            switch (status)
            {
                //case ScenarioVehicleExistance.Unknown:
                //    return String.Empty;
                //case ScenarioVehicleExistance.Exists:
                //    image = "BulletGreen.png";
                //    break;
                //case ScenarioVehicleExistance.MissingButInPreset:
                //case ScenarioVehicleExistance.Missing:
                default:
                    image = "BulletRed.png";
                    break;
            }
            string uri = String.Format("/LocoSwap;component/Resources/{0}", image);
            return uri;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
