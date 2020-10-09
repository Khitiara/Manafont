using System;

namespace Manafont.Packets
{
    [AttributeUsage(AttributeTargets.Struct)]
    [Serializable]
    public class DiscriminatedPacketAttribute : Attribute
    {
        public readonly ushort Opcode;

        public DiscriminatedPacketAttribute(ushort opcode) {
            Opcode = opcode;
        }
    }
}