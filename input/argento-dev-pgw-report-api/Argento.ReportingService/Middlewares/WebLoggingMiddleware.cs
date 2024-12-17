using Argento.ReportingService.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.Middlewares
{
    public sealed class WebLoggingMiddleware
    {

        private readonly RequestDelegate next;
        private readonly ILogger logger;

        public WebLoggingMiddleware(RequestDelegate next, ILogger<ArcadiaConstants.LoggerNames.Web> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            HttpRequest request = httpContext.Request;
            HttpResponse response = httpContext.Response;

            Stream originalResponseBody = response.Body;

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    response.Body = memoryStream;

                    // Do tasks before other middleware here, aka 'BeginRequest'
                    string activityId = null;
                    DateTime startDateTime = DateTime.Now;

                    if (httpContext.Request.Headers.TryGetValue(ArcadiaConstants.HeaderKeys.ActivitId,
                        out StringValues values))
                    {
                        activityId = values.FirstOrDefault();
                    }

                    if (string.IsNullOrEmpty(activityId))
                    {
                        activityId = Guid.NewGuid().ToString("N");
                    }

                    httpContext.Items[ArcadiaConstants.RequestScopeKeys.ActivityId] = activityId;

                    // Let the middleware pipeline run
                    await next(httpContext);

                    // Do tasks after middleware here, aka 'EndRequest'
                    response.Headers.Add(ArcadiaConstants.HeaderKeys.ActivitId, activityId);

                    var requestHeaders = new StringBuilder();
                    foreach (string key in request.Headers.Keys)
                    {
                        requestHeaders.Append($" {key}: ${request.Headers[key]};");
                    }

                    var responseHeaders = new StringBuilder();
                    foreach (string key in response.Headers.Keys)
                    {
                        responseHeaders.Append($" {key}: ${response.Headers[key]};");
                    }

                    string requestBody = string.Empty;
                    if (request.Body != null && request.Body.CanRead)
                    {
                        using (var requestMemoryStream = new MemoryStream())
                        {
                            await request.Body.CopyToAsync(requestMemoryStream).ConfigureAwait(false);
                            byte[] requestBytes = requestMemoryStream.ToArray();
                            requestBody = Encoding.UTF8.GetString(requestBytes);
                        }
                    }

                    byte[] responseBytes = memoryStream.ToArray();

                    string responseBody = string.Empty;
                    if (response.ContentType != null &&
                        response.ContentType.Trim().ToLower().Contains("application/json"))
                    {
                        responseBody = Encoding.UTF8.GetString(responseBytes);
                    }

                    await originalResponseBody.WriteAsync(responseBytes, 0, responseBytes.Length).ConfigureAwait(false);


                    TimeSpan executionTime = DateTime.Now - startDateTime;

                    var sb = new StringBuilder();
                    sb.AppendLine($"[URL] {request.Method} {response.StatusCode} {request.GetDisplayUrl()}");
                    sb.AppendLine("[REQUEST_HEADERS]" + requestHeaders);
                    sb.AppendLine("[REQUEST_BODY] " + requestBody);
                    sb.AppendLine("[RESPONSE_HEADERS]" + responseHeaders);
                    sb.AppendLine("[RESPONSE_BODY] " + responseBody);
                    sb.AppendLine($"[EXECUTION_TIME] {executionTime.TotalSeconds} seconds");

                    logger.LogInformation(sb.ToString());
                }
            }
            //catch (Exception e) { }
            finally
            {
                response.Body = originalResponseBody;
            }
        }

    }
}