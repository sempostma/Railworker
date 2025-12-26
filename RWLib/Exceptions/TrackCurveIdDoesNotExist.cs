using System.Runtime.Serialization;

namespace RWLib.Exceptions
{
    [Serializable]
    internal class TrackCurveIdDoesNotExist : Exception
    {
        public TrackCurveIdDoesNotExist()
        {
        }

        public TrackCurveIdDoesNotExist(string? message) : base(message)
        {
        }

        public TrackCurveIdDoesNotExist(string? message, Exception? innerException) : base(message, innerException)
        {
        }

    }
}