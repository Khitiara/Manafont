using Microsoft.Extensions.DependencyInjection;

namespace Manafont.Db
{
    public static class Extensions
    {
        public static IServiceCollection AddManafontDb(this IServiceCollection services) {
            services.AddDbContext<ManafontDbContext>();
            services
                .AddOpenIddict()
                .AddCore(options => {
                    options.UseEntityFrameworkCore()
                        .UseDbContext<ManafontDbContext>();
                });
            return services;
        }
    }
}