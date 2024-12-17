using Argento.ReportingService.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Argento.ReportingService.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseWebLogging(this IApplicationBuilder appBuilder)
        {
            return appBuilder.UseMiddleware<WebLoggingMiddleware>();
        }

    }
}