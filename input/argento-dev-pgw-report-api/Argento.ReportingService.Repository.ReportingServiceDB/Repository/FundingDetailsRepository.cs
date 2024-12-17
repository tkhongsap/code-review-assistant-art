using Arcadia.Extensions.DependencyInjection.Attributes;
using Arcadia.Extensions.DependencyInjection.Enums;
using Arcadia.Repository.EFCore;
using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Repository.Repository;

namespace Argento.ReportingService.Repository.ReportingServiceDB.Repository
{
    [RegisterType(typeof(IFundingDetailsRepository), DependencyLifeTime.Scoped)]
    public class FundingDetailsRepository : RepositoryBase<FundingDetailsEntity>, IFundingDetailsRepository
    {
        public FundingDetailsRepository(IUnitOfWorkReportingServiceDB unitOfWork) : base(unitOfWork)
        {
        }
    }
}
