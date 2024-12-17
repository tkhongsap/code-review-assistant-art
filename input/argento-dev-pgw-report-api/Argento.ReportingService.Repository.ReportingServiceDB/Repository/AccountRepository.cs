using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Repository.Repository;
using Arcadia.Extensions.DependencyInjection.Attributes;
using Arcadia.Extensions.DependencyInjection.Enums;
using Arcadia.Repository.EFCore;

namespace Argento.ReportingService.Repository.ReportingServiceDB.Repository
{
    [RegisterType(typeof(IAccountRepository), DependencyLifeTime.Scoped)]
    public class AccountRepository : RepositoryBase<AccountEntity>, IAccountRepository
    {
        public AccountRepository(IUnitOfWorkReportingServiceDB unitOfWork) : base(unitOfWork)
        {
        }
    }
}
