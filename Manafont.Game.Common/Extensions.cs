using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Manafont.Packets.IO;

namespace Manafont.Game.Common
{
    public static class Extensions
    {
        public static async ValueTask<T> ExpectPacketAsync<T>(this IManafontConnection connection,
            CancellationToken cancellationToken = default)
            where T : struct {
            IPacket pkt = await connection.RecvPacketAsync(cancellationToken);
            if (pkt is BasePacket<T> packet) {
                return packet.Data;
            }

            throw new IOException($"Unexpected packet kind: {pkt.PacketOpcode}");
        }
    }
}