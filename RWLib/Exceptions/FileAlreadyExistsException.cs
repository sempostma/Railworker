﻿using System.Runtime.Serialization;

namespace RWLib.Exceptions
{
    [Serializable]
    public class FileAlreadyExistsException : Exception
    {
        public FileAlreadyExistsException()
        {
        }

        public FileAlreadyExistsException(string? message) : base(message)
        {
        }

        public FileAlreadyExistsException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected FileAlreadyExistsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}