using System.Runtime.Serialization;

namespace RWLib.Exceptions
{
    [Serializable]
    internal class TrackTileRibbonIdIsNull : Exception
    {
        public TrackTileRibbonIdIsNull()
        {
        }

        public TrackTileRibbonIdIsNull(string? message) : base(message)
        {
        }

        public TrackTileRibbonIdIsNull(string? message, Exception? innerException) : base(message, innerException)
        {
        }

    }
}