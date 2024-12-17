using Argento.ReportingService.Repository.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Argento.ReportingService.BL.Interface
{
    public interface IFundingService
    {
        Task ApproveTransaction(List<Guid> selectedTransactionIds, Guid UserId);
    }
}
