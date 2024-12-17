using Argento.ReportingService.DL;
using Argento.ReportingService.DL.Helpers;
using Argento.ReportingService.DL.Reconciles;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Argento.ReportingService.BL.Interface
{
    public interface IReconcileService
    {
        Task<List<Dropdown>> GetDropDownReportTypes();
        Task<PagedList<ReconcilePagingDto>> GetReconcileProcess(ReconcilePagingParameters resourceParameter);
        Task<ReconcileProcessResponse> SaveReconcileProcessFromFile(ReconcileProcessSaveFromFileRequest resourceParameter, string userId);
        Task<ReconcileLastestProcess> GetLastReconcileProcess(string bankIssuer, string filename);
        Task<ReconcileProcessResponse> CancelReconcileProcessDetail(ReconcileCancelRequest resourceParameter, string userId);
        Task<ReconcileProcessResponse> CheckTransactionApprove(List<SettlementReportDetails> list);
        Task<ReconcileProcessResponse> CheckTransactionApproveFromFile(string fileUrl);
        Task<ReconcileProcessValidateFileResponse> ValidateFileFormat(string fileUrl);
    }
}
