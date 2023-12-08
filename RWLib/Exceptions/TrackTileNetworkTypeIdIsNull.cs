using System.Runtime.Serialization;

namespace RWLib.Exceptions
{
    [Serializable]
    internal class TrackTileNetworkTypeIdIsNull : Exception
    {
        public TrackTileNetworkTypeIdIsNull()
        {
        }

        public TrackTileNetworkTypeIdIsNull(string? message) : base(message)
        {
        }

        public TrackTileNetworkTypeIdIsNull(string? message, Exception? innerException) : base(message, innerException)
        {
        }

    }
}