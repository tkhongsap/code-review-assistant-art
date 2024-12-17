using System;

namespace Argento.ReportingService.DL.Transactions
{
    public class TransactionListDto
    {
        public string TransactionNo { get; set; }
        public string InvoiceNo { get; set; }
        public string TransactionDate { get; set; }
        public Guid MerchantId { get; set; }
        public string MerchantName { get; set; }
        public string PaymentChannel { get; set; }
        public decimal Amount { get; set; }
        public decimal? paidAmount { get; set; }
        public decimal Fee { get; set; }
        public decimal FeeVat { get; set; }
        public decimal Balance { get; set; }
        public int TransactionStatusId { get; set; }
        public string Description { get; set; }
        public string deviceProfileId { get; set; }
    }
}
