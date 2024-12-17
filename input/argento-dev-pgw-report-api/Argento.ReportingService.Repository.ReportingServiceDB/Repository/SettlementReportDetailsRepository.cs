using Arcadia.Extensions.DependencyInjection.Attributes;
using Arcadia.Extensions.DependencyInjection.Enums;
using Arcadia.Repository.EFCore;
using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Repository.Repository;

namespace Argento.ReportingService.Repository.ReportingServiceDB.Repository
{
    [RegisterType(typeof(ISettlementReportDetailsRepository), DependencyLifeTime.Scoped)]
    public class SettlementReportDetailsRepository : RepositoryBase<SettlementReportDetailsEntity>, ISettlementReportDetailsRepository
    {
        public SettlementReportDetailsRepository(IUnitOfWorkReportingServiceDB unitOfWork) : base(unitOfWork)
        {
        }
    }
}
