using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Manafont.Packets.IO
{
    static internal class ReflectExtensions
    {
        public static IntPtr GetOffset(this MemberInfo member) {
            return Marshal.OffsetOf(member.DeclaringType!, member.Name);
        }
    }
}