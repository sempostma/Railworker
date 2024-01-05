using System.Runtime.Serialization;

namespace RWLib.Exceptions
{
    [Serializable]
    internal class InvalidPackageLicenseException : Exception
    {
        public InvalidPackageLicenseException()
        {
        }

        public InvalidPackageLicenseException(string? message) : base(message)
        {
        }

        public InvalidPackageLicenseException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected InvalidPackageLicenseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}