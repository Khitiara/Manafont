using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AsyncBinaryExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Manafont.Packets.IO
{
    public class PacketIoOptions
    {
        private readonly Dictionary<ushort, Type> _opcodeRegistry = new Dictionary<ushort, Type>();
        private readonly HashSet<Type>            _packetTypes    = new HashSet<Type>();

        public void Register(IPacketSerializer serializer) {
            if (_packetTypes.Add(serializer.PacketType)) {
                _opcodeRegistry[serializer.Opcode] = serializer.PacketType;
            }
        }

        public Type GetPacketType(ushort opcode) => _opcodeRegistry[opcode];
    }

    public class PacketIo
    {
        private readonly IServiceProvider _serviceProvider;
        private          PacketIoOptions  _options;

        public PacketIo(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
            _options = new PacketIoOptions();
            foreach (IPacketSerializer serializer in serviceProvider.GetServices<IPacketSerializer>()) {
                _options.Register(serializer);
            }
        }

        public async ValueTask<IPacket> ReadPacketAsync(Stream stream, CancellationToken cancellationToken = default) {
            ushort opcode = await stream.ReadNetworkNumberAsync<ushort>(cancellationToken);
            Type packetType = _options.GetPacketType(opcode);
            IPacketSerializer serializer = (IPacketSerializer) _serviceProvider.GetRequiredService(
                typeof(PacketSerializer<>).MakeGenericType(packetType));
            return await serializer.ReadWrappedAsync(stream, cancellationToken);
        }

        public async ValueTask WritePacketAsync<T>(Stream stream, T packet,
            CancellationToken cancellationToken = default)
            where T : struct {
            PacketSerializer<T> serializer = _serviceProvider.GetRequiredService<PacketSerializer<T>>();
            await serializer.WriteAsync(stream, packet, cancellationToken);
        }
    }
}