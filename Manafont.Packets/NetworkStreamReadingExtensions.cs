using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AsyncBinaryExtensions;

namespace Manafont.Packets
{
    public static class NetworkStreamReadingExtensions
    {
        public static async ValueTask<T> ReadNetworkEndianNumberAsync<T>(this Stream stream,
            CancellationToken cancellationToken = default) where T : unmanaged {
            int size;
            unsafe {
                size = sizeof(T);
            }

            byte[] bytes = await stream.ReadBytesAsync(size, cancellationToken);
            return ReadUnaligned<T>(bytes);
        }

        private static T ReadUnaligned<T>(byte[] bytes) where T : unmanaged {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return Unsafe.ReadUnaligned<T>(ref bytes[0]);
        }
    }
}