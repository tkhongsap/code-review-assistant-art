using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.DL.Reconciles
{
    public class ReconcileProcessRequestData
    {
        public ReconcileProcess ReconcileProcess { get; set; }
        public List<ReconcileProcessDetail> ReconcileProcessDetail { get; set; }
    }

    public class ReconcileProcessRequestDataDetail
    {
        public string reportFileName { get; set; }
        public string reconcileProcessId { get; set; }

        public List<SettlementReportDetails> SettlementReportDetails { get; set; }
    }
    public class ReconcileProcess
    {
        public string ReportTypeId { get; set; }
        public string Issuer { get; set; }
        public string ReportFileName { get; set; }
        public string ReportFileUrl { get; set; }
        public string ProcessStatus { get; set; }
        public string Remark { get; set; }
        public decimal TotalRecord { get; set; }
        public decimal TotalAmount { get; set; }
    }
    public class ReconcileProcessDetail
    {
        public string PaymentMethod { get; set; }

        public DateTime? EstimatedCashInDate { get; set; }
    }

    public class SettlementReportDetails
    {
        public string ReferenceOrder { get; set; }
        public string SourceOfFund { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentType { get; set; }
        public DateTime? ReportDate { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime? ChargeDateTime { get; set; }
        public DateTime? AuthDateTime { get; set; }
        public decimal ExRate { get; set; }
        public decimal BahtAmount { get; set; }
        public decimal BahtCommAmount { get; set; }
        public decimal BahtVAT { get; set; }
        public decimal WithHoldingTax { get; set; }
        public decimal BahtNetAmount { get; set; }
        public decimal NetWithHoldingTax { get; set; }
    }

    public class CheckTransactionApproveRequest
    {
        public List<SettlementReportDetails> SettlementReportDetails { get; set; }
    }

    public class CheckTransactionApproveFromFileRequest
    {
        public string FileUrl { get; set; }
    }

    public class ReconcileFileData
    {
        public string TransactionNo { get; set; }
        public string TotalAmount { get; set; }
        public string CommissionAmount { get; set; }
        public string ComVATAmount { get; set; }
        public string NetReceiveFromBank { get; set; }
        public string WT { get; set; }
        public string NetAmountAfterWHT { get; set; }
        public string CreateDateTime { get; set; }
        public string AuthDateTime { get; set; }
    }
}
