using System.Threading;
using System.Threading.Tasks;

namespace Argento.ReportingService.Services
{
    internal interface IScopedAccountIntegrationService
    {
        Task DoWork(CancellationToken stoppingToken);
    }
}
