using Argento.ReportingService.BL.Interface;
using Argento.ReportingService.Controllers.Internal;
using Argento.ReportingService.DL.Reconciles;
using Argento.ReportingService.FilterAttributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Argento.ReportingService.Controllers
{
    [CheckAuthentication]
    [Route("api/v1/reconcile")]
    [ApiController]
    public class ReconcileController : ReportingServiceControllerBase
    {
        private readonly IReconcileService _reconcileService;
        public ReconcileController(IReconcileService reconcileService
            , IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            _reconcileService = reconcileService;
        }

        [HttpGet("report_type")]
        public async Task<IActionResult> DropDownReportType()
        {
            var collection = await _reconcileService.GetDropDownReportTypes();
            return Ok(collection);
        }

        [HttpGet("getReconcileProcess")]
        public async Task<IActionResult> GetReconcileProcess([FromQuery] ReconcilePagingParameters resourceParameter)
        {
            var collection = await _reconcileService.GetReconcileProcess(resourceParameter);
            return Ok(collection);
        }

        [HttpGet("getLastReconcileProcess")]
        public async Task<IActionResult> GetLastReconcileProcess([FromQuery] string bankIssuer, string filename)
        {
            var collection = await _reconcileService.GetLastReconcileProcess(bankIssuer, filename);
            return Ok(collection);
        }

        [HttpPost("cancelReconcileProcessDetail")]
        public async Task<IActionResult> CancelReconcileProcessDetail([FromBody] ReconcileCancelRequest resourceParameter)
        {
            var collection = await _reconcileService.CancelReconcileProcessDetail(resourceParameter, UserId);
            return Ok(collection);
        }

        [HttpPost("checkTransactionApprove")]
        public async Task<IActionResult> CheckTransactionApprove([FromBody] CheckTransactionApproveRequest resourceParameter)
        {
            var collection = await _reconcileService.CheckTransactionApprove(resourceParameter.SettlementReportDetails);
            return Ok(collection);
        }

        [HttpPost("checkTransactionApproveFromFile")]
        public async Task<IActionResult> CheckTransactionApproveFromFile([FromBody] CheckTransactionApproveFromFileRequest requestData)
        {
            var collection = await _reconcileService.CheckTransactionApproveFromFile(requestData.FileUrl);
            return Ok(collection);
        }

        [HttpPost("validate-file-format")]
        public async Task<IActionResult> ValidateReconcileTempFile([FromBody] CheckTransactionApproveFromFileRequest requestData)
        {
            var response = await _reconcileService.ValidateFileFormat(requestData.FileUrl);
            return Ok(response);
        }

        [HttpPost("save-reconcile-process-from-file")]
        public async Task<IActionResult> SaveReconcileProcessFromFile([FromBody] ReconcileProcessSaveFromFileRequest resourceParameter)
        {
            var collection = await _reconcileService.SaveReconcileProcessFromFile(resourceParameter, UserId);
            return Ok(collection);
        }

        [HttpGet("getDatetimeNow")]
        public async Task<IActionResult> GetDatetimeNow()
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
            DateTime targetLocalTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, targetTimeZone);
            return Ok($"Datetime UTC Now {DateTime.UtcNow} | DateTime Now {DateTime.Now} | Datetime UTC Convert  {targetLocalTime}");
        }
    }
}
