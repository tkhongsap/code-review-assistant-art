using System;

namespace Argento.ReportingService.DL.FundingTransfer
{
    public class FundingTransferListDto
    {
        public string PaymentDateTime { get; set; }
        public string InvoiceNo { get; set; }
        public Guid MerchantId { get; set; }
        public string MerchantCode { get; set; }
        public string SapCustomerId { get; set; }
        public string MerchantName { get; set; }
        public string SapCompanyCode { get; set; }

        public string BeneficiaryName { get; set; }
        public string BeneficiaryBankName { get; set; }
        public string BeneficiaryBankCode { get; set; }
        public string BeneficiaryAccountNo { get; set; }
        public decimal Amount { get; set; }
        public int TransferStatusId { get; set; }
        public string TransferStatusName { get; set; }
        public string SlipUrl { get; set; }
        public string DueDateTime { get; set; }
        public string AccountName { get; set; }
        public string BankCode { get; set; }
        public string BankBranch { get; set; }
        public string AccountNo { get; set; }
        public string FundingTransferReportNumber { get; set; }
        public string BankStatus { get; set; }
        public string BankName { get; set; }
        public string SettlementDateTime { get; set; }
    }
}
