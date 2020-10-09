using System;

namespace Manafont.Packets
{
    public readonly struct BasePacket<T> : IPacket where T : unmanaged
    {
        public BasePacket(ushort packetOpcode, T data) {
            PacketOpcode = packetOpcode;
            Data = data;
        }

        public ushort PacketOpcode { get; }
        public T Data { get; }
        
        public TPacket ExtractPacket<TPacket>() where TPacket : unmanaged {
            Type target = typeof(TPacket);
            if (!target.IsAssignableFrom(typeof(T))) {
                throw new InvalidOperationException($"Cannot extract a value of type {target.FullName} " +
                                                    $"from a packet containing {typeof(T).FullName}");
            }

            return (TPacket) (object) Data;
        }
    }
}