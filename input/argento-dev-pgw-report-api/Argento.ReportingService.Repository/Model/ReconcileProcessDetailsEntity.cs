using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.Repository.Model
{
    public class ReconcileProcessDetailsEntity : MasterDataEntityBase
    {
        public Guid ReconcileProcessId { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string PaymentMethod { get; set; }
        
        public DateTime? EstimatedCashInDate { get; set; }
    }
}
