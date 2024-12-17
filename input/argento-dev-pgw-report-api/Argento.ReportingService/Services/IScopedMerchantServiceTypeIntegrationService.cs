using System.Threading;
using System.Threading.Tasks;

namespace Argento.ReportingService.Services
{
    internal interface IScopedMerchantServiceTypeIntegrationService
    {
        Task DoWork(CancellationToken stoppingToken);
    }
}
