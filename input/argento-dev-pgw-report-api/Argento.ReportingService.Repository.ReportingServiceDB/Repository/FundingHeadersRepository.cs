using Arcadia.Extensions.DependencyInjection.Attributes;
using Arcadia.Extensions.DependencyInjection.Enums;
using Arcadia.Repository.EFCore;
using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Repository.Repository;

namespace Argento.ReportingService.Repository.ReportingServiceDB.Repository
{
    [RegisterType(typeof(IFundingHeadersRepository), DependencyLifeTime.Scoped)]
    public class FundingHeadersRepository : RepositoryBase<FundingHeadersEntity>, IFundingHeadersRepository
    {
        public FundingHeadersRepository(IUnitOfWorkReportingServiceDB unitOfWork) : base(unitOfWork)
        {
        }
    }
}
