using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Manafont.Packets.IO;

namespace Manafont.Packets.Handling
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ClosingSessionPacket
    {
        public const ushort Opcode = 1;
        // no fields
    }

    public sealed class ClosingSessionPacketHandler : BasePacketHandler<ClosingSessionPacket, BasicPacketState>
    {
        protected override bool CheckOpcode(ushort opcode) {
            return opcode == 1;
        }

        protected override Task HandlePacketAsync(ClosingSessionPacket packet, BasicPacketState state,
            CancellationToken cancellationToken = default) {
            state.CancelProcessing();
            return Task.CompletedTask;
        }
    }
}