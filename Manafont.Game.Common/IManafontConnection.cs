using System.Threading;
using System.Threading.Tasks;
using Manafont.Packets.IO;

namespace Manafont.Game.Common
{
    public interface IManafontConnection
    {
        public ValueTask SendPacketAsync<T>(T packet, CancellationToken cancellationToken)
            where T : struct;

        public ValueTask<IPacket> RecvPacketAsync(CancellationToken cancellationToken);
    }
}