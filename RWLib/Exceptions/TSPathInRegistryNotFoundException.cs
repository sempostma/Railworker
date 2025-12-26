using System.Runtime.Serialization;

namespace RWLib.Exceptions
{
    [Serializable]
    public class TSPathInRegistryNotFoundException : Exception
    {
        private static string extraMessage = "Consider using the JustTrains RailWorks Registry Tool";

        public TSPathInRegistryNotFoundException()
        {
        }

        public TSPathInRegistryNotFoundException(string? message): base(message + " " + extraMessage)
        {
        }

        public TSPathInRegistryNotFoundException(string? message, Exception? innerException) : base(message + " " + extraMessage, innerException)
        {
        }

    }
}