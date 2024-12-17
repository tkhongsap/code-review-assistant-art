using System.Threading.Tasks;
using Argento.ReportingService.BL.Interface;
using Argento.ReportingService.DL.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace Argento.ReportingService.Controllers;


[Route("api/v1/auto-transaction")]
[ApiController]
public class AutoTransactionController : Controller
{
    private readonly ITransactionService _transactionService;

    public AutoTransactionController(ITransactionService transactionService)
    {
        this._transactionService = transactionService;
    }

    [HttpGet("auto-sending-transaction-report/accounting")]
    public async Task<IActionResult> SendTransactionReportAccounting([FromQuery] TransactionAutoSendingReportrequest request)
    {
        try
        {
            await _transactionService.TransactionAutoSendingReportExcel(request);
            return Ok();
        }
        catch (System.Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}