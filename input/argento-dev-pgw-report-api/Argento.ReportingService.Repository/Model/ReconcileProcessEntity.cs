using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.Repository.Model
{
    public class ReconcileProcessEntity : MasterDataEntityBase
    {
        public Guid ReportTypeId { get; set; }
        [Column(TypeName = "varchar(3)")]
        public string Issuer { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string ReportFileName { get; set; }
        [Column(TypeName = "varchar(500)")]
        public string ReportFileUrl { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string ProcessStatus { get; set; }
        [Column(TypeName = "varchar(200)")]
        public string Remark { get; set; }
        [Column(TypeName = "decimal(19, 4)")]
        public decimal TotalRecord { get; set; }
        [Column(TypeName = "decimal(19, 4)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string ReportNo { get; set; }
    }
}
