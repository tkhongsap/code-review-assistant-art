using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.DL.Transactions
{
    public class DashboardRequest
    {
        public Guid MerchantId { get; set; }
        public string PaymentChannel { get; set; }
    }
}
