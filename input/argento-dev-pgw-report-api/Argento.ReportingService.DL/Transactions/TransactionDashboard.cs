using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.DL.Transactions
{
    public class TransactionDashboard
    {
        public decimal AllTransaction { get; set; }
        public decimal AllTransactionPaid { get; set; }
        public decimal AllPaidAmount { get; set; }
        public decimal AllPaidAmountToday { get; set; }
        public string DatetimePOC { get; set; } 

        public string[] PaymentChannel { get; set; }
        public decimal[] PaymentChannelValuePerCent { get; set; }
        public decimal[] PaymentChannelValue { get; set; }
        public string[] DateLine { get; set; }
        public decimal[] DateLineValue { get; set; }
        public decimal AllDateLineValue { get; set; }

    }
}
