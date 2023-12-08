using System.Runtime.Serialization;

namespace RWLib.Exceptions
{
    [Serializable]
    internal class UknownCurveTypeException : Exception
    {
        public UknownCurveTypeException()
        {
        }

        public UknownCurveTypeException(string? message) : base(message)
        {
        }

        public UknownCurveTypeException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}