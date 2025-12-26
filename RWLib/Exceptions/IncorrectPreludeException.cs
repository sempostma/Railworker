using System;
using System.Runtime.Serialization;

namespace RWLib.Exceptions
{
    [Serializable]
    internal class IncorrectPreludeException : Exception
    {
        public IncorrectPreludeException()
        {
        }

        public IncorrectPreludeException(string? message) : base(message)
        {
        }

        public IncorrectPreludeException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

    }
}