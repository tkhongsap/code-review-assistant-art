using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Argento.ReportingService.Repository.Model
{
    public class FundingDetailsEntity : MasterDataEntityBase
    {

        [Column(TypeName = "uuid")]
        public Guid FundingId { get; set; }

        [Column(TypeName = "varchar(30)")]
        public string ReferenceNo { get; set; }

        public DateTime DueDateTime { get; set; }

        public DateTime? SettlementDateTime { get; set; }

        [Column(TypeName = "decimal(15,4)")]
        public decimal Amount { get; set; }
    }
}
