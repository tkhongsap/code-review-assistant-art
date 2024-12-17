using Argento.ReportingService.BL.Interface;
using Argento.ReportingService.DL.FundingTransfer;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Argento.ReportingService.Controllers.Internal;
using Argento.ReportingService.FilterAttributes;
using Microsoft.AspNetCore.Http;
using Argento.ReportingService.DL.Helpers;

namespace Argento.ReportingService.Controllers
{
    [CheckAuthentication]
    [Route("api/v1/transfer")]
    [ApiController]
    public class FundingTransferController : ReportingServiceControllerBase
    {
        private readonly IFundingTransferService _fundingTransferService;

        public FundingTransferController(IFundingTransferService fundingTransferService, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            _fundingTransferService = fundingTransferService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] FundingTransferResourceParameter resourceParameter)
        {
            try
            {
                PagedList<FundingTransferListDto> result = await _fundingTransferService.Get(resourceParameter);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }

        [HttpGet("export_raw")]
        public async Task<IActionResult> GetExport([FromQuery] FundingTransferResourceParameter resourceParameter)
        {
            try
            {
                var result = await _fundingTransferService.GetExport(resourceParameter);
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(500);
            }
        }

    }
}
