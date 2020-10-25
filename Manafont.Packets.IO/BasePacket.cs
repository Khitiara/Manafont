namespace Manafont.Packets.IO
{
    public readonly struct BasePacket<T> : IPacket
        where T : struct
    {
        public BasePacket(ushort packetOpcode, T data) {
            PacketOpcode = packetOpcode;
            Data = data;
        }

        public ushort PacketOpcode { get; }
        object IPacket.PacketData => Data;
        public T Data { get; }
    }
}