using RWLib.RWBlueprints.Components;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Railworker.DataRepository
{
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class VehicleVariation
    {
        public string BlueprintID { get; set; } = "";
        public byte[] BinContents { get; set; } = [];

    }
}
