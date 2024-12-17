using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.DL.Reconciles
{
    public class ReconcileLastestProcess
    {
        public string ProcessfinishDateTime { get; set; }
        public string ProcessStatus { get; set; }
        public decimal TotalRecord { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
