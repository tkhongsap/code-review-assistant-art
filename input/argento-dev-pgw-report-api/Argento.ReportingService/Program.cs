using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using NLog.Web;

namespace Argento.ReportingService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseNLog(new NLogAspNetCoreOptions()
                {
                    CaptureMessageTemplates = true,
                    CaptureMessageProperties = true,
                    RegisterHttpContextAccessor = true
                })
                .UseStartup<Startup>();
    }
}
