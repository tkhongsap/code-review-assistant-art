using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Argento.ReportingService.Repository.Model
{
    public class FundingHeadersEntity : MasterDataEntityBase
    {

        public DateTime DueDateTime { get; set; }

        public DateTime? SettlementDateTime { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string BatchNo { get; set; }

        [Column(TypeName = "varchar(14)")]
        public string FundingTransferReportNo { get; set; }

        [Column(TypeName = "varchar(30)")]
        public string SendingBankCode { get; set; }

        public int TotalTransactionInBatch { get; set; }

        [Column(TypeName = "decimal(15, 4)")]
        public decimal TotalAmount { get; set; }

        public int FundingStatusId { get; set; }

        // move from detail to header
        public Guid MerchantId { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string MerchantCode { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string MerchantName { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string SapCompanyCode { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string BeneficiaryName { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string BeneficiaryEmail { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string BeneficiaryPhone { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string BeneficiaryBankName { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string BeneficiaryBankBranch { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string BeneficiaryBankCode { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string BeneficiaryAccountNo { get; set; }

    }
}
