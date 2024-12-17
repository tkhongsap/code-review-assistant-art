using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Argento.ReportingService.Services
{
    public class KafkaHostMerchantIntegrationService : BackgroundService
    {
        private readonly ILogger<KafkaHostMerchantIntegrationService> _logger;

        public KafkaHostMerchantIntegrationService(
            ILogger<KafkaHostMerchantIntegrationService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            Services = services;
        }

        public IServiceProvider Services { get; }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KafkaHostMerchantIntegrationService.ExecuteAsync init");

            _ = Task.Run(() => DoWork(stoppingToken), stoppingToken);

            return Task.CompletedTask;
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KafkaHostMerchantIntegrationService.DoWork init");

            using (var scope = Services.CreateScope())
            {
                var scopedKafkaService = scope.ServiceProvider.GetRequiredService<IScopedMerchantIntegrationService>();

                await scopedKafkaService.DoWork(stoppingToken);
            }
        }
    }
}
