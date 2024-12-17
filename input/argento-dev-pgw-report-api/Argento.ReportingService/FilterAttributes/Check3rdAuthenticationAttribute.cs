using Argento.ReportingService.BL.Interface;
using Argento.ReportingService.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Argento.ReportingService.FilterAttributes
{
    public class Check3rdAuthenticationAttribute : TypeFilterAttribute
    {
        public Check3rdAuthenticationAttribute() : base(typeof(Check3rdAuthenticationFilter))
        {
        }
    }

    public class Check3rdAuthenticationFilter : IAuthorizationFilter
    {
        private readonly AppSettings appSettings;
        private readonly IMerchantService merchantService;
        private const string AuthorizationHeaderName = "Authorization";
        private const string AuthenticationSchema = "Bearer";

        public Check3rdAuthenticationFilter(IOptions<AppSettings> appSettingsOptions
            , IMerchantService merchantService)
        {
            appSettings = appSettingsOptions.Value;
            this.merchantService = merchantService;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            try
            {
                var requestPath = context.HttpContext.Request.Path.ToString();

                if (requestPath.ToLower().Contains("/payment/source") || requestPath.Contains("/payment/getTransaction") || requestPath.Contains("/payment/cancel"))
                {
                    string authorization = context.HttpContext.Request.Headers[AuthorizationHeaderName].ToString()?.Trim();

                    if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith(AuthenticationSchema))
                    {
                        var authHeader = AuthenticationHeaderValue.Parse(context.HttpContext.Request.Headers[AuthorizationHeaderName]);
                        var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter));

                        var selected = merchantService.ValidateSecretKey(credentials).Result;

                        if (selected == "")
                        {
                            context.Result = new StatusCodeResult((int)HttpStatusCode.Unauthorized);
                            return;
                        }

                        context.HttpContext.Items[ArcadiaConstants.RequestScopeKeys.MerchantId] = selected;
                        return;
                    }
                }

                if (requestPath.ToLower().Contains("/payment/charge"))
                {
                    return;
                }
                if (requestPath.ToLower().Contains("/payment/validate"))
                {
                    return;
                }
                if (requestPath.ToLower().Contains("/payment/test"))
                {
                    return;
                }

                context.Result = new StatusCodeResult((int)HttpStatusCode.Unauthorized);
            }
            catch (Exception)
            {

            }
        }
    }
}
