using System.IO;
using Manafont.Packets.Handling;

namespace Manafont.Game.Common
{
    public sealed class ManafontGameClientContext : BasicPacketState
    {
        public ManafontGameClientContext(Stream stream) : base(stream) { }
    }
}