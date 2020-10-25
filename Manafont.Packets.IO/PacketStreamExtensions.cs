using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Manafont.Packets.IO
{
    public static class PacketStreamExtensions
    {
        public static async ValueTask<T> ReadNetworkNumberAsync<T>(this StreamReadBuffer runningBuffer,
            CancellationToken cancellationToken = default)
            where T : unmanaged, IConvertible {
            int size;
            unsafe {
                size = sizeof(T);
            }

            Memory<byte> bytes = await runningBuffer.ReadAsync(size, cancellationToken);
            if (BitConverter.IsLittleEndian)
                bytes.Span.Reverse();
            return Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(bytes.Span));
        }

        public static async ValueTask<T> ReadNetworkNumberAsync<T>(this Stream stream,
            CancellationToken cancellationToken = default)
            where T : unmanaged, IConvertible {
            int size;
            unsafe {
                size = sizeof(T);
            }

            StreamReadBuffer runningBuffer = new StreamReadBuffer(new byte[size], stream);
            return await ReadNetworkNumberAsync<T>(runningBuffer, cancellationToken);
        }

        public static async ValueTask<DateTime> ReadNetworkDateTimeAsync(this StreamReadBuffer runningBuffer,
            CancellationToken cancellationToken = default) {
            long binary = await runningBuffer.ReadNetworkNumberAsync<long>(cancellationToken);
            return DateTime.FromBinary(binary);
        }

        public static ValueTask WriteNetworkDateTimeAsync(this Stream stream, DateTime dateTime,
            CancellationToken cancellationToken = default) =>
            stream.WriteNetworkNumberAsync(dateTime.ToBinary(), cancellationToken);

        public static async ValueTask WriteNetworkNumberAsync<T>(this Stream stream, T t,
            CancellationToken cancellationToken = default)
            where T : unmanaged, IConvertible {
            int size;

            unsafe {
                size = sizeof(T);
            }

            Memory<byte> bytes = new byte[size];
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(bytes.Span), t);
            if (BitConverter.IsLittleEndian)
                bytes.Span.Reverse();

            await stream.WriteAsync(bytes, cancellationToken);
        }

        // ByValTStr marshaling with ascii encoding, stripped of trailing '\0'
        public static async ValueTask<string> ReadAnsiCStringAsync(this StreamReadBuffer buffer,
            int stringBlockSize, CancellationToken cancellationToken = default) {
            Memory<byte> memory = await buffer.ReadAsync(stringBlockSize, cancellationToken);
            return Encoding.ASCII.GetString(memory.Span).TrimEnd('\0');
        }

        public static async ValueTask WriteAnsiCStringAsync(this Stream stream, string value, int stringBlockSize,
            CancellationToken cancellationToken = default) {
            Memory<byte> memory = new byte[stringBlockSize];
            Encoding.ASCII.GetBytes(value, memory.Span);
            await stream.WriteAsync(memory, cancellationToken);
        }
    }
}