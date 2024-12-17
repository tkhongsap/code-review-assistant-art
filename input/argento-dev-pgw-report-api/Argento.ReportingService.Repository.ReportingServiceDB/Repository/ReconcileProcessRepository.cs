using Arcadia.Extensions.DependencyInjection.Attributes;
using Arcadia.Extensions.DependencyInjection.Enums;
using Arcadia.Repository.EFCore;
using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Repository.Repository;

namespace Argento.ReportingService.Repository.ReportingServiceDB.Repository
{
    [RegisterType(typeof(IReconcileProcessRepository), DependencyLifeTime.Scoped)]
    public class ReconcileProcessRepository : RepositoryBase<ReconcileProcessEntity>, IReconcileProcessRepository
    {
        public ReconcileProcessRepository(IUnitOfWorkReportingServiceDB unitOfWork) : base(unitOfWork)
        {
        }
    }
}
