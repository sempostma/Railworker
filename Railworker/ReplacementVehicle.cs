using Railworker.Core;

namespace Railworker
{
    public class ReplacementVehicle : Vehicle, IReplacable
    {
        public string Name => throw new System.NotImplementedException();

        public string BinPath => throw new System.NotImplementedException();
    }
}