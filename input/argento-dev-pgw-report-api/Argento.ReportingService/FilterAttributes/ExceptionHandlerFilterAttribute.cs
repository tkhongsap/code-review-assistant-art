using Argento.ReportingService.BL.CustomHttpExceptions;
using Argento.ReportingService.Extensions;
using Argento.ReportingService.Utility;
using Argento.ReportingService.Utility.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Argento.ReportingService.FilterAttributes
{
    public sealed class ExceptionHandlerFilterAttribute : ExceptionFilterAttribute
    {

        private readonly ILogger logger;

        public ExceptionHandlerFilterAttribute(ILogger<ArcadiaConstants.LoggerNames.Error> logger)
        {
            this.logger = logger;
        }

        //public override Task OnExceptionAsync(ExceptionContext context)
        //{
        //    logger.LogError(context.Exception.ToErrorLogs());
        //    context.HttpContext.ResponseErrorHeaders(context.Exception, out string error);

        //    context.HttpContext.Response.ContentType = "application/json";
        //    var stringEror = error;
        //    Microsoft.AspNetCore.Http.HttpResponseWritingExtensions.WriteAsync(context.HttpContext.Response, stringEror);

        //    context.ExceptionHandled = true;

        //    return base.OnExceptionAsync(context);
        //}
        public override void OnException(ExceptionContext context)
        {
            logger.LogError(context.Exception.ToErrorLogs());

            var statusCode = HttpStatusCode.InternalServerError;
            var respCode = "9999";
            var respDesc = context.Exception.Message;

            if (context.Exception is ICustomHttpException exception)
            {
                statusCode = exception.StatusCode;
                respCode = exception.RespCode;
                respDesc = exception.RespDesc;
            }

            context.HttpContext.Response.ContentType = "application/json";
            context.HttpContext.Response.StatusCode = (int)statusCode;

            context.Result = new JsonResult(new { respCode, respDesc });
        }
    }
}