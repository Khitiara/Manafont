using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Manafont.Config
{
    public static class Extensions
    {
        public static IServiceCollection AddManafontConfig(this IServiceCollection services) {
            services.TryAdd(ServiceDescriptor.Singleton(sp => sp.GetRequiredService<IConfiguration>()
                .GetSection("Manafont").Get<ManafontConfig>()));
            return services;
        }
    }
}