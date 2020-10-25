using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Manafont.Packets.IO
{
    public static class ReflectExtensions
    {
        public static IntPtr GetOffset(this MemberInfo member) {
            return Marshal.OffsetOf(member.DeclaringType!, member.Name);
        }

        public static IServiceCollection AddPacketSerializer<TPkt, TSer>(this IServiceCollection services, TSer ser)
            where TPkt : struct
            where TSer : PacketSerializer<TPkt> {
            services.TryAddSingleton<PacketIo>();
            return services.AddSingleton(ser)
                .AddSingleton<PacketSerializer<TPkt>>(ser)
                .AddSingleton<IPacketSerializer>(ser);
        }

        public static IServiceCollection AddPacketSerializer<TPkt, TSer>(this IServiceCollection services)
            where TPkt : struct
            where TSer : PacketSerializer<TPkt> {
            services.TryAddSingleton<PacketIo>();
            return services.AddSingleton<TSer>()
                .AddSingleton<PacketSerializer<TPkt>>(sp => sp.GetRequiredService<TSer>())
                .AddSingleton<IPacketSerializer>(sp => sp.GetRequiredService<TSer>());
        }
    }
}