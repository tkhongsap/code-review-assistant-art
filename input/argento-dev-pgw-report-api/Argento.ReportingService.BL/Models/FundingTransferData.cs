using Argento.ReportingService.Repository.Model;
using System.Collections.Generic;

namespace Argento.ReportingService.BL.Models
{
    public class FundingTransferData
    {
        public FundingHeadersEntity Header { get; set; }
        public List<FundingDetailsEntity> Details { get; set; }
        public List<TransactionEntity> Transactions { get; set; }
    }
}
