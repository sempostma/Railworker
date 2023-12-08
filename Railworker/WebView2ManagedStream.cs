using System;
using System.IO;

namespace Railworker
{
    class WebView2ManagedStream : Stream
    {
        public WebView2ManagedStream(Stream s)
        {
            s_ = s;
        }

        public override bool CanRead => s_.CanRead;

        public override bool CanSeek => s_.CanSeek;

        public override bool CanWrite => s_.CanWrite;

        public override long Length => s_.Length;

        public override long Position { get => s_.Position; set => s_.Position = value; }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return s_.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            try
            {
                read = s_.Read(buffer, offset, count);
                if (read == 0)
                {
                    s_.Dispose();
                }
            }
            catch
            {
                s_.Dispose();
                throw;
            }
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        private Stream s_;
    }
}
