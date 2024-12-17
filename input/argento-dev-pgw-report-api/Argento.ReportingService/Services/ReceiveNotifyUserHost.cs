using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Argento.ReportingService.Services
{
    public class ReceiveNotifyUserHost : BackgroundService
    {
        private readonly ILogger<ReceiveNotifyUserHost> _logger;

        public ReceiveNotifyUserHost(
            ILogger<ReceiveNotifyUserHost> logger
            , IServiceProvider services)
        {
            _logger = logger;
            Services = services;
        }

        public IServiceProvider Services { get; }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReceiveNotifyUserHost.ExecuteAsync init");

            _ = Task.Run(() => DoWork(stoppingToken), stoppingToken);

            return Task.CompletedTask;
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReceiveNotifyUserHost.DoWork init");

            using (var scope = Services.CreateScope())
            {
                var scopedKafkaService = scope.ServiceProvider.GetRequiredService<IReceiveNotifyUserService>();

                await scopedKafkaService.DoWork(stoppingToken);
            }
        }
    }
}
