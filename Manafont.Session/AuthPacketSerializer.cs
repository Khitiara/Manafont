using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Manafont.Packets.IO;

namespace Manafont.Session
{
    public sealed class AuthPacketSerializer : PacketSerializer<AuthPacket>
    {
        public AuthPacketSerializer() : base(0x48, AuthPacket.Opcode) { }

        protected override async ValueTask ReadFieldsAsync(StreamReadBuffer buffer, AuthPacket[] pkt,
            CancellationToken cancellationToken = default) {
            await buffer.EnsureAsync(0x48, cancellationToken);
            pkt[0].OAuthToken = await buffer.ReadAnsiCStringAsync(0x28, cancellationToken);
            int major = await buffer.ReadNetworkNumberAsync<int>(cancellationToken);
            int minor = await buffer.ReadNetworkNumberAsync<int>(cancellationToken);
            int build = await buffer.ReadNetworkNumberAsync<int>(cancellationToken);
            int rev = await buffer.ReadNetworkNumberAsync<int>(cancellationToken);
            pkt[0].ProtocolVersion = new Version(major, minor, build, rev);
        }

        protected override async ValueTask WriteFieldsAsync(Stream stream, AuthPacket pkt,
            CancellationToken cancellationToken = default) {
            await stream.WriteAnsiCStringAsync(pkt.OAuthToken, 0x28, cancellationToken);
            await stream.WriteNetworkNumberAsync(pkt.ProtocolVersion.Major, cancellationToken);
            await stream.WriteNetworkNumberAsync(pkt.ProtocolVersion.Minor, cancellationToken);
            await stream.WriteNetworkNumberAsync(pkt.ProtocolVersion.Build, cancellationToken);
            await stream.WriteNetworkNumberAsync(pkt.ProtocolVersion.Revision, cancellationToken);
        }
    }
}