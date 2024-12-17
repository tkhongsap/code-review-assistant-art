﻿using Arcadia.Extensions.DependencyInjection.Attributes;
using Arcadia.Extensions.DependencyInjection.Enums;
using Arcadia.Repository.EFCore;
using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Repository.Repository;

namespace Argento.ReportingService.Repository.ReportingServiceDB.Repository
{
    [RegisterType(typeof(IReconcileProcessDetailsRepository), DependencyLifeTime.Scoped)]
    public class ReconcileProcessDetailsRepository : RepositoryBase<ReconcileProcessDetailsEntity>, IReconcileProcessDetailsRepository
    {
        public ReconcileProcessDetailsRepository(IUnitOfWorkReportingServiceDB unitOfWork) : base(unitOfWork)
        {
        }
    }
}
