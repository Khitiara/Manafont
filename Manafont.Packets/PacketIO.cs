using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AsyncBinaryExtensions;

namespace Manafont.Packets
{
    public class PacketIO
    {
        private readonly Dictionary<ushort, Type> _opcodeRegistry = new Dictionary<ushort, Type>();
        public IReadOnlyDictionary<ushort, Type> OpcodeRegistry { get; }

        public PacketIO() {
            OpcodeRegistry = new ReadOnlyDictionary<ushort, Type>(_opcodeRegistry);
        }
        
        
        public async ValueTask<IPacket> ReadPacketAsync(Stream stream, CancellationToken cancellationToken) {
            ushort opcode = await stream.ReadNetworkEndianNumberAsync<ushort>(cancellationToken);
            Type type = OpcodeRegistry[opcode];
            int size = Marshal.SizeOf(type);
            Memory<byte> mem = await stream.ReadBytesAsync(size, cancellationToken);
            object pkt = ReflectionExtensions.GetLoader(type)(mem.Span);
            Type fullPacket = typeof(BasePacket<>).MakeGenericType(type);
            return (IPacket) Activator.CreateInstance(fullPacket, opcode, pkt)!;
        }
    }
}