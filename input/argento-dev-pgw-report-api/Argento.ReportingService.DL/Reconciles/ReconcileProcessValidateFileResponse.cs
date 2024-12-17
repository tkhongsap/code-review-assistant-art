using System.Collections.Generic;

namespace Argento.ReportingService.DL.Reconciles
{
    public class ReconcileProcessValidateFileResponse
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalRecord { get; set; }
    }
}
