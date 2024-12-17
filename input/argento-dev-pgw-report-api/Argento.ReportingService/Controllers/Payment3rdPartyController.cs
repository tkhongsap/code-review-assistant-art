using Argento.ReportingService.BL.Interface;
using Argento.ReportingService.Controllers.Internal;
using Argento.ReportingService.DL.Transactions;
using Argento.ReportingService.FilterAttributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Argento.ReportingService.Controllers
{
    [Check3rdAuthenticationAttribute]
    [Route("api/v1/payment")]
    [ApiController]
    public class Payment3rdPartyController : ReportingServiceControllerBase
    {
        private readonly ITransactionService _transactionService;

        public Payment3rdPartyController(
            IHttpContextAccessor httpContextAccessor,
            ITransactionService transactionService) : base(httpContextAccessor)
        {
            _transactionService = transactionService;
        }

        [HttpPost]
        [Route("getTransaction")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> GetTransaction(TransactionRequestDto request)
        {
            var result = await _transactionService.GetTransactionByRequest(request, MerchantIdContext);
            return Ok(result);
        }

        [HttpGet]
        [Route("getTransactionByNo")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public IActionResult GetTransactionByNo(string transactionNo)
        {
            var result = _transactionService.GetTransactionByTransactionNo(MerchantIdContext, transactionNo);

            return Ok(result);
        }
    }
}
