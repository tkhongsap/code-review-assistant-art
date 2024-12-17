using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Argento.ReportingService.DL.Utils;

namespace Argento.ReportingService.DL.Transactions
{
    public class TransactionAutoSendingReportrequest
    // : IValidatableObject
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }

    }
}