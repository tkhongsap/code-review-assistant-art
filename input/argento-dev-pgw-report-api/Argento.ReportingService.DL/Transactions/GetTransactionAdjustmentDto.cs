using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.DL.Transactions
{
    public class GetTransactionAdjustmentDto
    {
        public Guid TransactionId { get; set; }
        public string ServiceType { get; set; }
        public string CreateDateTime { get; set; }
        public string AuthorizeDateTime { get; set; }
        public string MerchantIdParent { get; set; }
        public string MerchantName { get; set; }
        public string SapCustomerId { get; set; }
        public string BusinessType { get; set; }
        public string SapInvoiceRef { get; set; }
        public string TransactionNo { get; set; }
        public string PaymentChannel { get; set; }
        public string Description { get; set; }
        public string SapInternalOrder { get; set; }
        public string MerchantOrderNo { get; set; }
        public string SourceOfFund { get; set; }
        public decimal? PayAmount { get; set; }
        public decimal? FeeAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? WHT { get; set; }
        public decimal NetAmount { get; set; }
        public string PaymentType { get; set; }
        public string PaymentStatus { get; set; }
        public string ProviderName { get; set; }
        public string PaybackDate { get; set; }
        public string TransferInfrom { get; set; }
        public string CashReceiveDate { get; set; }
        public string ReconcileReportNo { get; set; }
        public decimal Amount { get; set; }
        public decimal NetAmountWHT { get; set; }
        public string CancelDatetime { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? MdrTotal { get; set; }
        public int PaymentTerm { get; set; }
        public string AdjustDateTime { get; set; }
    }
}
