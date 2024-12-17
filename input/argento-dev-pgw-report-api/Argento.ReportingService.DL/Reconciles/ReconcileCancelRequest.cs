using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.DL.Reconciles
{
    public class ReconcileCancelRequest
    {
        public string reconcileProcessId { get; set; }
        public string remark { get; set; }
    }
}
