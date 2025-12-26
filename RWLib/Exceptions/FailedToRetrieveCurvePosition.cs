using System.Runtime.Serialization;

namespace RWLib.Exceptions
{
    [Serializable]
    internal class FailedToRetrieveCurvePosition : Exception
    {
        public FailedToRetrieveCurvePosition()
        {
        }

        public FailedToRetrieveCurvePosition(string? message) : base(message)
        {
        }

        public FailedToRetrieveCurvePosition(string? message, Exception? innerException) : base(message, innerException)
        {
        }

    }
}