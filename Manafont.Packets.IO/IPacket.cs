namespace Manafont.Packets.IO
{
    /// <summary>
    /// Generic packet base class
    /// </summary>
    public interface IPacket
    {
        /// <summary>
        /// Packet opcode/discriminator. Used to differentiate between packet types.
        /// </summary>
        ushort PacketOpcode { get; }
        
        /// <summary>
        /// Un-type-safe access to packet data
        /// </summary>
        object PacketData { get; }
    }
}