using Arcadia.Extensions.DependencyInjection.Attributes;
using Arcadia.Extensions.DependencyInjection.Enums;
using Arcadia.Repository.EFCore;
using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Repository.Repository;

namespace Argento.ReportingService.Repository.ReportingServiceDB.Repository
{
    [RegisterType(typeof(IReportTypesRepository), DependencyLifeTime.Scoped)]
    public class ReportTypesRepository : RepositoryBase<ReportTypesEntity>, IReportTypesRepository
    {
        public ReportTypesRepository(IUnitOfWorkReportingServiceDB unitOfWork) : base(unitOfWork)
        {
        }
    }
}
