using System.Runtime.Serialization;

namespace RWLib.Exceptions
{
    [Serializable]
    internal class TSPathInRegistryNotFoundException : Exception
    {
        public TSPathInRegistryNotFoundException()
        {
        }

        public TSPathInRegistryNotFoundException(string? message) : base(message)
        {
        }

        public TSPathInRegistryNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected TSPathInRegistryNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}