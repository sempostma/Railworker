using Railworker.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Railworker.DataRepository
{
    public class LocalDataRepository : IDataRepository
    {
        public async Task StoreReplacementRule(ReplacementRule rule)
        {
            Settings.Default.ReplacementRules.List.Add(rule);
            Settings.Default.Save();
        }

        public async Task StoreVehicleVariation(VehicleVariation blueprint)
        {
            Settings.Default.VehicleVariations.List.Add(blueprint);
            Settings.Default.Save();
        }

        public async Task<ReplacementRules> GetReplacementRules()
        {
            return Settings.Default.ReplacementRules;
        }

        public async Task<VehicleVariations> GetVehicleVariations()
        {
            return Settings.Default.VehicleVariations;
        }
    }
}
