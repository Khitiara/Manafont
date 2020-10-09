using System;
using System.Threading;
using System.Threading.Tasks;
using Manafont.Packets.IO;

namespace Manafont.Packets.Handling
{
    public interface IPacketHandler<in TState>
        where TState : class
    {
        Task HandlePacketAsync(IPacket packet, TState state, CancellationToken cancellationToken = default);
    }

    public interface IPacketHandler<TPacket, in TState> : IPacketHandler<TState>
        where TPacket : unmanaged
        where TState : class
    {
        Task HandlePacketAsync(BasePacket<TPacket> packet, TState state, CancellationToken cancellationToken = default);
    }

    public abstract class BasePacketHandler<TPacket, TState> : IPacketHandler<TPacket, TState>, IPacketHandler<TState>
        where TState : BasicPacketState
        where TPacket : unmanaged
    {
        Task IPacketHandler<TState>.HandlePacketAsync(IPacket packet, TState state,
            CancellationToken cancellationToken) {
            return HandlePacketAsync((BasePacket<TPacket>) packet, state, cancellationToken);
        }

        public Task HandlePacketAsync(BasePacket<TPacket> packet, TState state,
            CancellationToken cancellationToken = default) {
            if (!CheckOpcode(packet.PacketOpcode))
                throw new InvalidOperationException($"Cannot handle opcode {packet.PacketOpcode}");
            return HandlePacketAsync(packet.Data, state, cancellationToken);
        }

        protected abstract bool CheckOpcode(ushort opcode);

        protected abstract Task HandlePacketAsync(TPacket packet, TState state,
            CancellationToken cancellationToken = default);
    }
}