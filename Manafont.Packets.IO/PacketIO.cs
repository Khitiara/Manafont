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

namespace Manafont.Packets.IO
{
    public class PacketIo
    {
        private readonly Dictionary<ushort, Type> _opcodeRegistry        = new Dictionary<ushort, Type>();
        private readonly Dictionary<Type, ushort> _reverseOpcodeRegistry = new Dictionary<Type, ushort>();
        private readonly object                   _registryLock          = new object();

        public ushort RegisterPacketType<T>() {
            Type type = typeof(T);
            lock (_registryLock) {
                if (_reverseOpcodeRegistry.ContainsKey(type))
                    return _reverseOpcodeRegistry[type];
            }

            DiscriminatedPacketAttribute? attribute = type.GetCustomAttribute<DiscriminatedPacketAttribute>();
            if (attribute is null)
                throw new InvalidOperationException($"Packet type {type.FullName} is not properly configured " +
                    "to be an opcode-discriminated packet, add the [DiscriminatedPacket] attribute.");
            ushort opcode = attribute.Opcode;
            lock (_registryLock) {
                _opcodeRegistry[opcode] = type;
                _reverseOpcodeRegistry[type] = opcode;
            }

            return opcode;
        }

        public void RegisterAllPackets(Assembly assembly, bool eagerBuildSerialization = false) {
            foreach (TypeInfo type in assembly.DefinedTypes.Where(i =>
                (i.IsPublic || i.IsNestedPublic) && (i.IsLayoutSequential || i.IsExplicitLayout))) {
                DiscriminatedPacketAttribute? attribute = type.GetCustomAttribute<DiscriminatedPacketAttribute>();
                if (attribute is null)
                    continue;
                ushort opcode = attribute.Opcode;
                lock (_registryLock) {
                    if (_opcodeRegistry.ContainsKey(opcode) && !_opcodeRegistry[opcode].Equals(type)) {
                        throw new InvalidOperationException("Cannot add multiple packets with the same opcode");
                    }

                    if (_reverseOpcodeRegistry.ContainsKey(type))
                        continue;
                    _opcodeRegistry[opcode] = type;
                    _reverseOpcodeRegistry[type] = opcode;
                }

                if (!eagerBuildSerialization) continue;
                PacketDeserializers.GetLoader(type);
                PacketSerializers.GetSaver(type);
            }
        }

        public async ValueTask<IPacket>
            ReadPacketAsync(Stream stream, CancellationToken cancellationToken = default) {
            ushort opcode = await stream.ReadNetworkEndianNumberAsync<ushort>(cancellationToken);
            Type type = _opcodeRegistry[opcode];
            int size = Marshal.SizeOf(type);
            Memory<byte> mem = await stream.ReadBytesAsync(size, cancellationToken);
            object pkt = PacketDeserializers.GetLoader(type)(mem.Span);
            Type fullPacket = typeof(BasePacket<>).MakeGenericType(type);
            return (IPacket) Activator.CreateInstance(fullPacket, opcode, pkt)!;
        }

        public async ValueTask WriteDiscriminatedPacketAsync<T>(Stream stream,
            BasePacket<T> packet,
            CancellationToken cancellationToken = default) where T : unmanaged {
            ushort opcode = packet.PacketOpcode;
            Type type = _opcodeRegistry[opcode];
            int size = Marshal.SizeOf(type);
            using IMemoryOwner<byte> rent = MemoryPool<byte>.Shared.Rent(size);
            Memory<byte> mem = rent.Memory;
            PacketSerializers.GetSaver(type)(packet.Data, mem.Span);
            await stream.WriteAsync(mem, cancellationToken);
        }

        public async ValueTask WritePacketAsync<T>(Stream stream, T packet, CancellationToken cancellationToken)
            where T : unmanaged =>
            await WriteDiscriminatedPacketAsync(stream, AddOpcode(packet), cancellationToken);

        private BasePacket<T> AddOpcode<T>(T packet) where T : unmanaged {
            return new BasePacket<T>(RegisterPacketType<T>(), packet);
        }
    }
}