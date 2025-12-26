using System;
using System.Runtime.Serialization;

namespace Exceptions
{
    [Serializable]
    internal class FileEditorCannotOpenThisFileExtension : Exception
    {
        public FileEditorCannotOpenThisFileExtension()
        {
        }

        public FileEditorCannotOpenThisFileExtension(string? message) : base(message)
        {
        }

        public FileEditorCannotOpenThisFileExtension(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected FileEditorCannotOpenThisFileExtension(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}