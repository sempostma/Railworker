using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWLib.SerzClone
{
    public class BinaryStreamReader : IDisposable
    {
        const int bufferLength = 4096;
        private int chunkIndex = -1;
        private int cursorIndex = 0;
        private byte[] readChunk;
        private Stream stream;
        private int readChunkLength = 0;
        private bool lastChunk = false;
        private bool finished = false;

        internal bool IsFinished => finished;

        internal int CurrentIndex => chunkIndex * bufferLength + cursorIndex;
        internal byte Current
        {
            get
            {
                if (cursorIndex >= readChunk.Length)
                {
                    throw new InvalidOperationException("Current value exceeds the buffer size");
                }
                if (cursorIndex < 0)
                {
                    throw new InvalidOperationException("Cursor index is a negative value");
                }
                return readChunk[cursorIndex];
            }
        }

        public BinaryStreamReader(Stream stream)
        {
            readChunk = new byte[bufferLength];
            this.stream = stream;
        }

        public async Task Start()
        {
            await ReadNextChunk();
        }

        private async System.Threading.Tasks.Task ReadNextChunk()
        {
            readChunkLength = await stream.ReadAsync(readChunk, 0, bufferLength);
            chunkIndex++;
            cursorIndex = 0;
            if (readChunkLength < bufferLength) lastChunk = true;
            if (readChunkLength == 0) finished = true;
        }

        public async Task<int> IncrementCurrentIndex()
        {
            return await MoveCurrentIndex(1);
        }

        public async Task<int> MoveCurrentIndex(int total)
        {
            int index = 0;
            while (index < total && !IsFinished)
            {
                var pending = total - index;
                var bufferSpaceLeft = readChunkLength - cursorIndex;

                if (pending >= bufferSpaceLeft)
                {
                    index += bufferSpaceLeft;
                    cursorIndex += bufferSpaceLeft;

                    if (lastChunk)
                    {
                        total = index;
                        finished = true;
                        return total;
                    }
                    else await ReadNextChunk();
                }
                else
                {
                    index += pending;
                    cursorIndex += pending;
                }
            }


            return total;
        }

        public async Task<byte[]> ReadBytes(int total)
        {
            var buffer = new byte[total];
            int index = 0;
            while (index < total && !IsFinished)
            {
                var pending = total - index;
                var bufferSpaceLeft = readChunkLength - cursorIndex;

                if (pending >= bufferSpaceLeft)
                {
                    if (bufferSpaceLeft > 0)
                    {
                        Array.Copy(readChunk, cursorIndex, buffer, index, bufferSpaceLeft);
                        index += bufferSpaceLeft;
                        cursorIndex += bufferSpaceLeft;
                    }

                    if (lastChunk)
                    {
                        total = index;
                        finished = true;
                        return buffer.Take(total).ToArray();
                    }
                    else await ReadNextChunk();
                }
                else
                {
                    if (pending > 0)
                    {
                        Array.Copy(readChunk, cursorIndex, buffer, index, pending);
                        index += pending;
                        cursorIndex += pending;
                    }
                }
            }

            return buffer;
        }


        public async Task<bool> ReadBool()
        {
            var result = Current == 0 ? false : true;
            await IncrementCurrentIndex();
            return result;
        }

        public async Task<byte> ReadUint8()
        {
            var result = Current;
            await IncrementCurrentIndex();
            return result;
        }

        public async Task<UInt16> ReadUint16()
        {
            var bytes = await ReadBytes(2);
            return BitConverter.ToUInt16(bytes);
        }

        public async Task<UInt32> ReadUint32()
        {
            var bytes = await ReadBytes(4);
            return BitConverter.ToUInt32(bytes);
        }

        public async Task<UInt64> ReadUint64()
        {
            var bytes = await ReadBytes(8);
            return BitConverter.ToUInt64(bytes);
        }

        public async Task<Int16> ReadInt16()
        {
            var bytes = await ReadBytes(2);
            return BitConverter.ToInt16(bytes);
        }

        public async Task<Int32> ReadInt32()
        {
            var bytes = await ReadBytes(4);
            return BitConverter.ToInt32(bytes);
        }

        public async Task<Int64> ReadInt64()
        {
            var bytes = await ReadBytes(8);
            return BitConverter.ToInt64(bytes);
        }

        public async Task<float> ReadFloat()
        {
            var bytes = await ReadBytes(4);
            return BitConverter.ToSingle(bytes);
        }

        public async Task<double> ReadDouble()
        {
            var bytes = await ReadBytes(8);
            return BitConverter.ToDouble(bytes);
        }


        public void Dispose()
        {
            stream.Dispose();
        }
    }
}
