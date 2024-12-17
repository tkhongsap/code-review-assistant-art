using System.Net;

namespace Argento.ReportingService.BL.CustomHttpExceptions
{
    public interface ICustomHttpException
    {
        public HttpStatusCode StatusCode { get; }
        public string RespCode { get; }
        public string RespDesc { get; }
    }
}
