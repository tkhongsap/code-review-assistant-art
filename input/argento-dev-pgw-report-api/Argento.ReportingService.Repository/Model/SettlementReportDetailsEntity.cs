using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.Repository.Model
{
    public class SettlementReportDetailsEntity : MasterDataEntityBase
    {
        public Guid ReconcileProcessId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string ReferenceOrder { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string SourceOfFund { get; set; }
        [Column(TypeName = "varchar(200)")]
        public string PaymentMethod { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string PaymentType { get; set; }
        public DateTime? ReportDate { get; set; }
        [Column(TypeName = "varchar(3)")]
        public string CurrencyCode { get; set; }
        public DateTime? ChargeDateTime { get; set; }
        public DateTime? AuthDateTime { get; set; }
        [Column(TypeName = "decimal(19, 4)")]
        public decimal ExRate { get; set; }
        [Column(TypeName = "decimal(19, 4)")]
        public decimal BahtAmount { get; set; }
        [Column(TypeName = "decimal(19, 4)")]
        public decimal BahtCommAmount { get; set; }
        [Column(TypeName = "decimal(19, 4)")]
        public decimal BahtVAT { get; set; }
        [Column(TypeName = "decimal(19, 4)")]
        public decimal WHT { get; set; }
        [Column(TypeName = "decimal(19, 4)")]
        public decimal BahtNetAmount { get; set; }

        [Column(TypeName = "decimal(19, 4)")]
        public decimal BahtNetWHTAmount { get; set; }

        [Column(TypeName = "varchar(14)")]
        public string BankReferenceOrder;

    }
}