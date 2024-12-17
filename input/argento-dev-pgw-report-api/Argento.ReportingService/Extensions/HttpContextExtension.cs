using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Argento.ReportingService.Utility;
using Argento.ReportingService.Utility.Exceptions;
using Argento.ReportingService.Utility.Extensions;
using Argento.ReportingService.DL;

namespace Argento.ReportingService.Extensions
{
    public static class HttpContextExtension
    {
        public static void ResponseErrorHeaders(this HttpContext httpContext, Exception exception, out string errorJsonString)
        {
            errorJsonString = string.Empty;
            HttpStatusCode statusCode;
            string activityId;

            activityId = httpContext.Items[ArcadiaConstants.RequestScopeKeys.ActivityId]?.ToString();
            if (exception is ArcadiaBusinessFlowException)
            {
                var response = new ResponseDto();
                var arcadiaBusinessFlowException = (ArcadiaBusinessFlowException)exception;

                statusCode = HttpStatusCode.OK;
                var errorDetail = new ErrorDetailDto()
                {
                    Action = arcadiaBusinessFlowException.Action,
                    Level = arcadiaBusinessFlowException.ExceptionLevel,
                    Errors = arcadiaBusinessFlowException.ErrorObject != null 
                                ? arcadiaBusinessFlowException.ErrorObject : 
                                arcadiaBusinessFlowException.Message
                };
                response.ActivityId = activityId;
                response.Error = errorDetail;
                response.Success = false;
                response.Data = null;

                errorJsonString = System.Text.Json.JsonSerializer.Serialize(response);
            }
            else
            {
                Exception ex = exception.GetException();
                statusCode = HttpStatusCode.InternalServerError;
            }

            httpContext.Response.StatusCode = (int)statusCode;
        }
    }
}