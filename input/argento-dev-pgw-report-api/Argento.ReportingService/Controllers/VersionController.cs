using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Argento.ReportingService.Controllers
{
    [Route("/")]
    [ApiController]
    public class VersionController : Controller
    {
        private readonly BuildVersion _buildVersion;

        public VersionController(IOptions<BuildVersion> buildVersion)
        {
            _buildVersion = buildVersion.Value;
        }

        [HttpGet("version")]
        public IActionResult version()
        {
            return Ok(_buildVersion.Version);
        }
    }
}
