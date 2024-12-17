using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Argento.ReportingService.Services
{
    internal class KafkaHostCallbackUrlService : BackgroundService
    {
        private readonly ILogger<KafkaHostCallbackUrlService> _logger;

        public KafkaHostCallbackUrlService(
            ILogger<KafkaHostCallbackUrlService> logger,
            IServiceProvider services
        )
        {
            _logger = logger;
            Services = services;
        }

        public IServiceProvider Services { get; }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KafkaWorkerHostService.ExecuteAsync init");

            _ = Task.Run(() => DoWork(stoppingToken), stoppingToken);

            return Task.CompletedTask;
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KafkaWorkerHostService.DoWork init");

            using (var scope = Services.CreateScope())
            {
                var scopedKafkaService = scope.ServiceProvider.GetRequiredService<IScopedCallbackUrlService>();

                await scopedKafkaService.DoWork(stoppingToken);
            }
        }
    }
}
