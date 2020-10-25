using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Manafont.Packets.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Manafont.Packets.Handling
{
    public static class Extensions
    {
        public static async IAsyncEnumerable<IPacket> ReadPacketsAsync(this PacketIo packetIo, Stream stream,
            [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            while (!cancellationToken.IsCancellationRequested) {
                yield return await packetIo.ReadPacketAsync(stream, cancellationToken);
            }
        }

        public static IServiceCollection AddPacketIo<TState>(this IServiceCollection services)
            where TState : BasicPacketState {
            services.TryAddSingleton<PacketIo>();
            services.TryAddSingleton<PacketHandlerManager<TState>>();
            services.AddPacketSerializer<ClosingSessionPacket, EmptyPacketSerializer<ClosingSessionPacket>>(
                new EmptyPacketSerializer<ClosingSessionPacket>(ClosingSessionPacket.Opcode));
            return services;
        }
    }
}