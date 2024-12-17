using System.Threading;
using System.Threading.Tasks;

namespace Argento.ReportingService.Services
{
    internal interface IScopedUserRoleIntegrationService
    {
        Task DoWork(CancellationToken stoppingToken);
    }
}
