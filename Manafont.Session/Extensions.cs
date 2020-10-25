using Manafont.Packets.IO;
using Microsoft.Extensions.DependencyInjection;

namespace Manafont.Session
{
    public static class Extensions
    {
        public static IServiceCollection AddManafontSession(this IServiceCollection services) {
            services.AddSingleton(sp => new ManafontSessionManager(sp));
            services.AddPacketSerializer<AuthPacket, AuthPacketSerializer>();
            services.AddPacketSerializer<SessionRevokedPacket, EmptyPacketSerializer<SessionRevokedPacket>>(
                new EmptyPacketSerializer<SessionRevokedPacket>(SessionRevokedPacket.Opcode));
            return services;
        }
    }
}