using Railworker.DataRepository;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Railworker.Properties
{
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class VehicleVariations
    {
        public ObservableCollection<VehicleVariation> List { get; } = new ObservableCollection<VehicleVariation>();
    }
}
