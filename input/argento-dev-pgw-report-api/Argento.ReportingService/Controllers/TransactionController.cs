using Argento.ReportingService.BL.Interface;
using Argento.ReportingService.DL.Transactions;
using Argento.ReportingService.FilterAttributes;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Argento.ReportingService.Controllers
{
    [CheckAuthentication]
    [Route("api/v1/transaction")]
    [ApiController]
    public class TransactionController : Controller
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            this._transactionService = transactionService;
        }

        [HttpGet]
        [Route("GetTransactionResult")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> GetTransactionResult([FromQuery] Guid id)
        {
            var result = await _transactionService.GetTransactionResult(id);

            return Ok(result);
        }

        [HttpGet]
        [Route("GetTransactionByPaymentMerchant/{merchantId}")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> GetTransactionByPaymentMerchant(Guid merchantId,
            [FromQuery] TransactionPagingParameters resourceParameter)
        {
            var result = await _transactionService.GetTransactionByPaymentMerchant(merchantId, resourceParameter);

            return Ok(result);
        }

        [HttpGet]
        [Route("GetTransactionByPaymentMerchantExport/{merchantId}")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> GetTransactionByPaymentMerchantExport(Guid merchantId,
            [FromQuery] TransactionPagingParameters resourceParameter)
        {
            try
            {
                var result =
                    await _transactionService.GetTransactionByPaymentMerchantExport(merchantId, resourceParameter);

                if (result == null || result.Length == 0)
                {
                    return BadRequest(result);
                }

                return File(result, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"TransactionReport_{DateTime.Now:YYYYMMDD_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetTransactionByPaymentAdmin")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> GetTransactionByPaymentAdmin(
            [FromQuery] TransactionPagingParameters resourceParameter)
        {
            var result = await _transactionService.GetTransactionByPaymentAdmin(resourceParameter);

            return Ok(result);
        }

        [HttpGet]
        [Route("GetTransactionByPaymentAdminTransfer")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> GetTransactionByPaymentAdminTransfer(
            [FromQuery] TransactionPagingParameters resourceParameter)
        {
            var result = await _transactionService.GetTransactionByPaymentAdminTransfer(resourceParameter);

            return Ok(result);
        }


        [HttpGet]
        [Route("GetTransactionDashBoard")]
        public async Task<IActionResult> GetTransactionDashBoard([FromQuery] DashboardRequest resourceParameter)
        {
            var result = await _transactionService.GetTransactionDashBoard(resourceParameter);

            return Ok(result);
        }

        [HttpGet]
        [Route("GetTransactionByPaymentAdminExcel")]
        public async Task<IActionResult> GetTransactionByPaymentAdminExcel(
            [FromQuery] TransactionPagingParameters resourceParameter)
        {
            try
            {
                var result = await _transactionService.GetTransactionByPaymentAdminExcel(resourceParameter);
                if (result == null || result.Length == 0)
                {
                    return BadRequest(result);
                }

                DateTime dateTimeNow = DateTime.Now;
                return File(result, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"TransactionReport_{DateTime.Now.ToString("YYYYMMDD_HHmmss")}.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("get-transaction-adjustment")]
        public async Task<IActionResult> GetTransactionAdjustment([FromQuery] GetTransactionAdjustmentRequest resourceParameter)
        {
            var result = await _transactionService.GetTransactionAdjustment(resourceParameter);
            return Ok(result);
        }
    }
}
