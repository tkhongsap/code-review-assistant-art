using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Linq;
using System.Net;

namespace Argento.ReportingService.FilterAttributes
{
    public class ValidationFilterAttribute : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                //context.Result = new UnprocessableEntityObjectResult(context.ModelState);

                var statusCode = HttpStatusCode.UnprocessableEntity;
                var respCode = "0401";
                var respDesc = context.ModelState.Keys
                .SelectMany(key => context.ModelState[key].Errors.Select(x => new ValidationError(key, x.ErrorMessage)))
                .ToList();

                context.HttpContext.Response.ContentType = "application/json";
                context.HttpContext.Response.StatusCode = (int)statusCode;

                context.Result = new JsonResult(new { respCode, respDesc });
            }
        }
        public void OnActionExecuted(ActionExecutedContext context) { }
    }

    public class ValidationError
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Field { get; }

        public string Message { get; }

        public ValidationError(string field, string message)
        {
            Field = field != string.Empty ? field : null;
            Message = message;
        }
    }
}
