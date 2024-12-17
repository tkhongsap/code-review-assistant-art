using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Argento.ReportingService.Services
{
    public class KafkaHostUserRoleIntegrationService : BackgroundService
    {
        private readonly ILogger<KafkaHostUserRoleIntegrationService> _logger;

        public KafkaHostUserRoleIntegrationService(
            ILogger<KafkaHostUserRoleIntegrationService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            Services = services;
        }

        public IServiceProvider Services { get; }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KafkaHostUserRoleIntegrationService.ExecuteAsync init");

            _ = Task.Run(() => DoWork(stoppingToken), stoppingToken);

            return Task.CompletedTask;
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KafkaHostUserRoleIntegrationService.DoWork init");

            using (var scope = Services.CreateScope())
            {
                var scopedKafkaService = scope.ServiceProvider.GetRequiredService<IScopedUserRoleIntegrationService>();

                await scopedKafkaService.DoWork(stoppingToken);
            }
        }
    }
}
