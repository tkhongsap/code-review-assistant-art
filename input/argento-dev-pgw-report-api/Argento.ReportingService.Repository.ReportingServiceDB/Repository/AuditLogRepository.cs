using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Repository.Repository;
using Arcadia.Extensions.DependencyInjection.Attributes;
using Arcadia.Extensions.DependencyInjection.Enums;
using Arcadia.Repository.EFCore;
using Microsoft.Extensions.Logging;

namespace Argento.ReportingService.Repository.ReportingServiceDB.Repository
{
    [RegisterType(typeof(IAuditLogRepository), DependencyLifeTime.Scoped)]
    public class AuditLogRepository : RepositoryBase<AuditLogEntity>, IAuditLogRepository
    {
        public AuditLogRepository(IUnitOfWorkReportingServiceDB unitOfWork) : base(unitOfWork)
        {
        }
    }
}
