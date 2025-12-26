using Railworker.Properties;
using System.Threading.Tasks;

namespace Railworker.DataRepository
{
    public interface IDataRepository
    {
        Task<ReplacementRules> GetReplacementRules();
        Task<VehicleVariations> GetVehicleVariations();
        Task StoreReplacementRule(ReplacementRule rule);
        Task StoreVehicleVariation(VehicleVariation blueprint);
    }
}