using System.Runtime.InteropServices;
using Manafont.Packets.IO;

namespace Manafont.Session
{
    [StructLayout(LayoutKind.Sequential)]
    [DiscriminatedPacket(Opcode)]
    public readonly struct AuthPacket
    {
        public const ushort Opcode = 2;
        
        public AuthPacket(string oAuthToken) {
            OAuthToken = oAuthToken;
        }

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public readonly string OAuthToken;
    }
}