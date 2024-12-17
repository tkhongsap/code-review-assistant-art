using System;

namespace Argento.ReportingService.DL.Transactions
{
    public class TransactionResultPageDto
    {
        public Guid Id { get; set; }

        public string PaymentDateTime { get; set; }
        public string Result { get; set; }
        public string CardNumber { get; set; }

        public decimal Amount { get; set; }
        public bool IsSuccess { get; set; }
        public string TransactionNo { get; set; }
        public string InvoiceNo { get; set; }
        public string PayeeName { get; set; }
        public string ArgentoToken { get; set; }
    }
}
