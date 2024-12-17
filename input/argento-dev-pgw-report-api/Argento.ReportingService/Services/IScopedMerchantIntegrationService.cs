using System.Threading;
using System.Threading.Tasks;

namespace Argento.ReportingService.Services
{
    internal interface IScopedMerchantIntegrationService
    {
        Task DoWork(CancellationToken stoppingToken);
    }
}
