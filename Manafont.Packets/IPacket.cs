namespace Manafont.Packets
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
        /// Extract the data chunk from a packet
        /// </summary>
        /// <exception cref="System.InvalidOperationException">If the specified target type is incorrect.</exception>
        /// <typeparam name="TPacket">Packet type to extract</typeparam>
        /// <returns>The extracted packet data segment</returns>
        TPacket ExtractPacket<TPacket>() where TPacket : unmanaged;
    }
}