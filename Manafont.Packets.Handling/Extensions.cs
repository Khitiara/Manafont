using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Manafont.Packets.IO;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddSingleton<PacketIo>();
            services.AddSingleton<PacketHandlerManager<TState>>();
            return services;
        }
    }
}