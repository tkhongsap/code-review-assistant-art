using Arcadia.Extensions.DependencyInjection.Attributes;
using Arcadia.Extensions.DependencyInjection.Enums;
using Arcadia.Repository.EFCore;
using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Repository.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.Repository.ReportingServiceDB.Repository
{
    [RegisterType(typeof(ITransactionRepository), DependencyLifeTime.Scoped)]
    public class TransactionRepository : RepositoryBase<TransactionEntity>, ITransactionRepository
    {
        public TransactionRepository(IUnitOfWorkReportingServiceDB unitOfWork) : base(unitOfWork)
        {
        }
    }
}
