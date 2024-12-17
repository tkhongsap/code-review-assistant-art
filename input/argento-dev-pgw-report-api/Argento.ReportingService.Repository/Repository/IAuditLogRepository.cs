using Argento.ReportingService.Repository.Model;
using Arcadia.Repository.EFCore;

namespace Argento.ReportingService.Repository.Repository
{
    public interface IAuditLogRepository : IRepository<AuditLogEntity>
    {
    }
}
