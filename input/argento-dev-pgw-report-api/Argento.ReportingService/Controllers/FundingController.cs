using Argento.ReportingService.BL.Interface;
using Argento.ReportingService.FilterAttributes;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Argento.ReportingService.DL.Funding;
using Argento.ReportingService.Controllers.Internal;
using Microsoft.AspNetCore.Http;

namespace Argento.ReportingService.Controllers
{
    [CheckAuthentication]
    [Route("api/v1/funding")]
    [ApiController]
    public class FundingController : ReportingServiceControllerBase
    {
        private readonly IFundingService fundingService;

        public FundingController(IFundingService fundingService, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.fundingService = fundingService;
        }

        [HttpPost]
        [Route("approve_transaction")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> ApproveTransaction([FromBody] ApproveTransaction resource)
        {
            await fundingService.ApproveTransaction(resource.TransactionList, Guid.Parse(UserId));

            return Ok();
        }
    }
}
