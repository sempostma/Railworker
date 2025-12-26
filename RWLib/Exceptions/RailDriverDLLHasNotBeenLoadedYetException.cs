using System.Runtime.Serialization;

namespace RWLib.Exceptions
{
    [Serializable]
    internal class RailDriverDLLHasNotBeenLoadedYetException : Exception
    {
        public RailDriverDLLHasNotBeenLoadedYetException()
        {
        }

        public RailDriverDLLHasNotBeenLoadedYetException(string? message) : base(message)
        {
        }

        public RailDriverDLLHasNotBeenLoadedYetException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected RailDriverDLLHasNotBeenLoadedYetException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}