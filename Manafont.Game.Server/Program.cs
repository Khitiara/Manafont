using Manafont.Config;
using Manafont.Db;
using Manafont.Game.Common;
using Manafont.Packets.Handling;
using Manafont.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Manafont.Game.Server
{
    public class Program
    {
        public static void Main(string[] args) {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) => {
                    services.AddManafontConfig();
                    services.AddManafontDb();
                    services.AddManafontSession();
                    services.AddPacketIo<ManafontGameClientContext>();
                    services.AddHostedService<Worker>();
                });
    }
}