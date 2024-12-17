using System;
using System.Text.Json.Serialization;

namespace Argento.ReportingService.DL.Transactions
{
    public class TransactionPagingDto
    {
        public Guid transactionId { get; set; }

        public Guid MerchantId { get; set; }
        public string merchantName { get; set; }
        public string merchantCode { get; set; }
        public string mainBranchId { get; set; }
        public string transactionNo { get; set; }
        public string InvoiceNo { get; set; }
        public string transactionDate { get; set; }
        public string paymentChannel { get; set; }
        public decimal amount { get; set; }
        public decimal? paidAmount { get; set; }
        public decimal? fee { get; set; }
        public decimal? vat { get; set; }
        public decimal? mdr { get; set; }
        public decimal? transferAmount { get; set; }
        public decimal? withHoldingTax { get; set; }
        public decimal? netAmount { get; set; }
        public int TransactionStatusId { get; set; }
        public string transactionStatusName { get; set; }
        public string source { get; set; }
        public string sapCustomerId { get; set; }
        public string merchantCategoryName { get; set; }
        public string invoiceRef { get; set; }
        public string transferDateTime { get; set; }
        public string issuer { get; set; }
        public string issuerCode { get; set; }
        public string estimatedCashInDate { get; set; }
        public string bahtAmount { get; set; }
        public string bahtCommAmount { get; set; }
        public string bahtVAT { get; set; }
        public string bahtNetAmount { get; set; }
        public string wt { get; set; }
        public string wtNetAmount { get; set; }
        public string description { get; set; }
        public int? merchantServiceType { get; set; }
        public string internalOrder { get; set; }
        public string bankReferenceOrder { get; set; }
        public string reportName { get; set; }
        public string payBackDate { get; set; }
        public string fundingTransferReportNo { get; set; }
        public string approveStatusName { get; set; }
        public int? approveStatusId { get; set; }
        public int? paymentTerm { get; set; }
        public string accountName { get; set; }
        public string accountNo { get; set; }
        public string bankName { get; set; }
        public string bankBranch { get; set; }
        public string sapCompanyCode { get; set; }
        public string reconcileReportNo { get; set; }
        public string authDateTime { get; set; }
        public string providerName { get; set; }
        public string paidAt { get; set; }
        public string chargeId {  get; set; }
    }

    public class TransactionTemp
    {
        public Guid transactionId { get; set; }

        public Guid MerchantId { get; set; }
        public string merchantName { get; set; }
        public string merchantCode { get; set; }
        public string mainBranchId { get; set; }
        public string transactionNo { get; set; }
        public string InvoiceNo { get; set; }
        public string transactionDate { get; set; }
        public string paymentChannel { get; set; }
        public decimal amount { get; set; }
        public decimal? paidAmount { get; set; }
        public decimal? fee { get; set; }
        public decimal? vat { get; set; }
        public decimal? mdr { get; set; }
        public decimal? feeAmount { get; set; }
        public decimal? transferAmount { get; set; }
        public decimal? withHoldingTax { get; set; }
        public decimal? netAmount { get; set; }
        public int TransactionStatusId { get; set; }
        public string transactionStatusName { get; set; }
        public string source { get; set; }
        public string sapCustomerId { get; set; }
        public string merchantCategoryName { get; set; }
        public string invoiceRef { get; set; }
        public string transferDateTime { get; set; }
        public string issuer { get; set; }
        public string issuerCode { get; set; }
        public string estimatedCashInDate { get; set; }
        public string bahtAmount { get; set; }
        public string bahtCommAmount { get; set; }
        public string bahtVAT { get; set; }
        public string bahtNetAmount { get; set; }
        public string wt { get; set; }
        public string wtNetAmount { get; set; }
        public int? merchantServiceType { get; set; }
        public string internalOrder { get; set; }
        public string bankReferenceOrder { get; set; }
        public string reportName { get; set; }
        public string payBackDate { get; set; }
        public Guid? fundingDetailId { get; set; }
        public Guid? fundingHeaderId { get; set; }
        public int? approveStatusId { get; set; }
        public string bankCode { get; set; }
        public string sapCompanyCode { get; set; }
        public string chargeId{ get; set; }
    }
}
