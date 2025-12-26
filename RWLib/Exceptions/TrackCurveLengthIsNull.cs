using System.Runtime.Serialization;

namespace RWLib.Exceptions
{
    [Serializable]
    internal class TrackCurveLengthIsNull : Exception
    {
        public TrackCurveLengthIsNull()
        {
        }

        public TrackCurveLengthIsNull(string? message) : base(message)
        {
        }

        public TrackCurveLengthIsNull(string? message, Exception? innerException) : base(message, innerException)
        {
        }

    }
}