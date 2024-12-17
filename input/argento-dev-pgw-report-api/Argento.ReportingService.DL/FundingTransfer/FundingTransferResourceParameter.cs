using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.DL.FundingTransfer
{
    public class FundingTransferResourceParameter : BaseResourceParameter
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public List<string> Bank { get; set; }
        public List<string> TransferStatus { get; set; }
    }
}
