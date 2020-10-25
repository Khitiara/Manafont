using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Manafont.Packets.IO
{
    public struct StreamReadBuffer
    {
        private Memory<byte> _memory;
        private Stream       _stream;

        public Memory<byte> Memory => _memory.Slice(0, Size);
        public int Size;
        public int Offset;

        public StreamReadBuffer(Memory<byte> span, Stream stream) {
            _memory = span;
            _stream = stream;
            Size = 0;
            Offset = 0;
        }

        public async ValueTask<bool> EnsureAsync(int count,
            CancellationToken cancellationToken = default) {
            if (count > _memory.Length)
                throw new ArgumentException("Cannot request more data than the buffer can store",
                    nameof(count));
            while (Size < count) {
                int numRead = await _stream.ReadAsync(_memory, cancellationToken)
                    .ConfigureAwait(false);
                Size += numRead;
                if (numRead == 0 && Size < count)
                    return false;
            }

            return true;
        }

        public async ValueTask<Memory<byte>> ReadAsync(int count,
            CancellationToken cancellationToken = default) {
            if (!await EnsureAsync(Offset + count, cancellationToken))
                throw new EndOfStreamException();
            Offset += count;
            return _memory.Slice(Offset - count, count);
        }
    }
}