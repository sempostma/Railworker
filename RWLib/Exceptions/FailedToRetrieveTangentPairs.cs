using System.Runtime.Serialization;

namespace RWLib.Exceptions
{
    [Serializable]
    internal class FailedToRetrieveTangentPairs : Exception
    {
        public FailedToRetrieveTangentPairs()
        {
        }

        public FailedToRetrieveTangentPairs(string? message) : base(message)
        {
        }

        public FailedToRetrieveTangentPairs(string? message, Exception? innerException) : base(message, innerException)
        {
        }

    }
}