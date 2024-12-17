using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.DL.Reconciles
{
    public class ReconcilePagingDto
    {
        public string ProcessfinishDateTime { get; set; }
        public string ReportTypeName { get; set; }
        public string IssuerCode { get; set; }
        public string IssuerName { get; set; }
        public string ReportFileName { get; set; }
        public decimal TotalRecord { get; set; }
        public decimal TotalAmount { get; set; }
        public string ProcessBy { get; set; }
        public string ProcessStatus { get; set; }
        public string ReconcileReportNo { get; set; }
        public string Remark { get; set; }
    }
}
