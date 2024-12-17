using System.Threading;
using System.Threading.Tasks;

namespace Argento.ReportingService.Services
{
    internal interface IScopedCallbackUrlService
    {
        Task DoWork(CancellationToken stoppingToken);
    }
}
