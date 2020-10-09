using Microsoft.Extensions.DependencyInjection;

namespace Manafont.Session
{
    public static class Extensions
    {
        public static IServiceCollection AddManafontSession(this IServiceCollection services) {
            services.AddSingleton(sp => new ManafontSessionManager(sp));
            return services;
        }
    }
}