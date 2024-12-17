using System.Collections.Generic;

namespace Argento.ReportingService.DL.Reconciles
{
    public class ReconcileProcessSaveFromFileRequest
    {
        public string FileUrl { get; set; }
        public string FileName { get; set; }
        public string ReportTypeId { get; set; }
        public string Issuer { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalRecord { get; set; }
        public List<ReconcileProcessDetail> Details { get; set; }
    }
}
