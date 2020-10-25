using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Manafont.Packets.IO
{
    public interface IPacketSerializer
    {
        public Type PacketType { get; }
        public ushort Opcode { get; }

        internal ValueTask<IPacket> ReadWrappedAsync(Stream stream, CancellationToken cancellationToken = default);
    }

    public abstract class PacketSerializer<T> : IPacketSerializer
        where T : struct
    {
        protected readonly int Size;
        public Type PacketType => typeof(T);
        public ushort Opcode { get; }

        async ValueTask<IPacket> IPacketSerializer.
            ReadWrappedAsync(Stream stream, CancellationToken cancellationToken) =>
            new BasePacket<T>(Opcode, await ReadAsync(stream, cancellationToken));

        protected PacketSerializer(int size, ushort opcode) {
            Size = size;
            Opcode = opcode;
        }

        protected abstract ValueTask ReadFieldsAsync(StreamReadBuffer buffer, T[] pkt,
            CancellationToken cancellationToken = default);

        protected abstract ValueTask WriteFieldsAsync(Stream stream, T pkt,
            CancellationToken cancellationToken = default);

        public async ValueTask<T> ReadAsync(Stream stream,
            CancellationToken cancellationToken = default) {
            using IMemoryOwner<byte> rent = MemoryPool<byte>.Shared.Rent(Size);
            StreamReadBuffer buffer = new StreamReadBuffer(rent.Memory, stream);
            T pkt = new T();
            await ReadFieldsAsync(buffer, new[] {pkt}, cancellationToken);
            return pkt;
        }

        public async ValueTask WriteAsync(Stream stream, T pkt,
            CancellationToken cancellationToken = default) {
            await stream.WriteNetworkNumberAsync(Opcode, cancellationToken);
            await WriteFieldsAsync(stream, pkt, cancellationToken);
        }
    }
    
    public class EmptyPacketSerializer<T> : PacketSerializer<T>
        where T : struct
    {
        public EmptyPacketSerializer(ushort opcode) : base(0, opcode) { }
        protected override ValueTask ReadFieldsAsync(StreamReadBuffer buffer, T[] pkt, CancellationToken cancellationToken = default) {
            // no - op
            return ValueTask.CompletedTask;
        }

        protected override ValueTask WriteFieldsAsync(Stream stream, T pkt, CancellationToken cancellationToken = default) {
            // no - op
            return ValueTask.CompletedTask;
        }
    }
}