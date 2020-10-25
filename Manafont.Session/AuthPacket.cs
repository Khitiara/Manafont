using System;
using System.Runtime.InteropServices;
using Manafont.Packets.IO;

namespace Manafont.Session
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AuthPacket
    {
        public const ushort Opcode = 2;

        public AuthPacket(string oAuthToken, Version version) {
            OAuthToken = oAuthToken;
            ProtocolVersion = version;
        }

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x28)]
        public string OAuthToken;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public Version ProtocolVersion;
    }
}