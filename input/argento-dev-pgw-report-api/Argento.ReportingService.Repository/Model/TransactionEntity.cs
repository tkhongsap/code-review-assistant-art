using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.Repository.Model
{
    public class TransactionEntity : MasterDataEntityBase
    {
        //[DatabaseGenerated(DatabaseGeneratedOption.Computed)]

        [Column(TypeName = "varchar(50)")]
        public string TransactionNo { get; set; }

        public Guid MerchantId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string MerchantCode { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string MerchantName { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string PaymentChannel { get; set; }

        [Column(TypeName = "decimal(15, 4)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(15, 4)")]
        public decimal? PaidAmount { get; set; }

        [Column(TypeName = "decimal(15, 4)")]
        public decimal Mdr { get; set; }

        [Column(TypeName = "decimal(15, 4)")]
        public decimal Fee { get; set; }

        [Column(TypeName = "decimal(15, 4)")]
        public decimal FeeVat { get; set; }

        [Column(TypeName = "decimal(15, 4)")]
        public decimal Vat { get; set; }

        [Column(TypeName = "decimal(15, 4)")]
        public decimal Balance { get; set; }
        public bool Paid { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? CancelAt { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string PaidCode { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string PaidMessage { get; set; }
        public int TransactionStatusId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Reference1 { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Reference2 { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Reference3 { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Description { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string ChargeId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string OrderId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string CardMasking { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string InvoiceNo { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string SourceName { get; set; }
        public int ApproveStatusId { get; set; }

        [Column(TypeName = "decimal(15, 4)")]
        public decimal? TransferAmount { get; set; }
        public DateTime? TransferDateTime { get; set; }
        public int? TransferStatusId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string InvoiceRef { get; set; }

        [Column(TypeName = "varchar(3)")]
        public string BankCode { get; set; }

        public Guid? MainBranchId { get; set; }

        [Column(TypeName = "decimal(15, 4)")]
        public decimal WithHoldingTax { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string DeviceProfileId { get; set; }

        public int? MerchantServiceType { get; set; }
        
        [Column(TypeName = "varchar(13)")]
        public string InternalOrder { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string ReconcileStatus { get; set; }

        public Guid? FundingDetailId { get; set; }
    }
}
