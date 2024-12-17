using System.Threading;
using System.Threading.Tasks;

namespace Argento.ReportingService.Services
{
    internal interface IReceiveNotifyUserService
    {
        Task DoWork(CancellationToken stoppingToken);
    }
}
