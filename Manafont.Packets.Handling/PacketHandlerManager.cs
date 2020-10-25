using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Manafont.Packets.IO;

namespace Manafont.Packets.Handling
{
    public class PacketHandlerManager<TState>
        where TState : BasicPacketState
    {
        public Dictionary<ushort, IPacketHandler<TState>> PacketHandlers { get; } =
            new Dictionary<ushort, IPacketHandler<TState>>();

        private readonly PacketIo _packetIo;

        public PacketHandlerManager(PacketIo packetIo) {
            _packetIo = packetIo;
        }

        public async ValueTask ProcessPackets(TState state,
            CancellationToken cancellationToken = default) =>
            await RunPacketProcessingLoop(_packetIo, PacketHandlers, state, cancellationToken);

        public static async ValueTask RunPacketProcessingLoop(PacketIo packetIo,
            Dictionary<ushort, IPacketHandler<TState>> handlers, TState state,
            CancellationToken cancellationToken = default) {
            CancellationTokenSource linked =
                CancellationTokenSource.CreateLinkedTokenSource(state.EndPacketProcessingToken, cancellationToken);
            await foreach (IPacket packet in packetIo.ReadPacketsAsync(state.Stream, linked.Token)) {
                if (!handlers.TryGetValue(packet.PacketOpcode, out IPacketHandler<TState>? handler)) {
                    continue;
                }

                await handler.HandlePacketAsync(packet, state, linked.Token);
            }
        }
    }
}