using System;
using System.Collections.Generic;
using System.Net;
using JWT.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Argento.ReportingService.DL.Users;
using Argento.ReportingService.Utility;
using JWT.Algorithms;

namespace Argento.ReportingService.FilterAttributes
{
    public class CheckAuthenticationAttribute : TypeFilterAttribute
    {
        public CheckAuthenticationAttribute() : base(typeof(CheckAuthenticationFilter))
        {
        }
    }

    public class CheckAuthenticationFilter : IAuthorizationFilter
    {
        private readonly AppSettings appSettings;

        private const string AuthorizationHeaderName = "Authorization";
        private const string AuthenticationSchema = "Bearer";

        public CheckAuthenticationFilter(IOptions<AppSettings> appSettingsOptions)
        {
            appSettings = appSettingsOptions.Value;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            string authorization = context.HttpContext.Request.Headers[AuthorizationHeaderName].ToString()?.Trim();
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith(AuthenticationSchema))
            {
                int indexOfSpace = authorization.IndexOf(' ');
                string jwt = authorization;
                if (indexOfSpace != -1)
                {
                    jwt = jwt.Substring(indexOfSpace + 1);
                }

                try
                {
                    var payload = new JwtBuilder()
                        .WithAlgorithm(new HMACSHA256Algorithm())
                        //.WithSecret(appSettings.EncryptionKey)
                        //.MustVerifySignature()
                        .Decode<IDictionary<string, object>>(jwt);
                    //var requestedUser = JsonConvert.DeserializeObject<RequestedUser>(payload["user"].ToString());
                    //context.HttpContext.Items[ArcadiaConstants.RequestScopeKeys.RequestedByUser] = requestedUser;
                    context.HttpContext.Items[ArcadiaConstants.RequestScopeKeys.MerchantId] = payload["merchant_id"].ToString();
                    context.HttpContext.Items[ArcadiaConstants.RequestScopeKeys.UserId] = payload["sub"].ToString();
                    return;
                }
                catch
                {
                    // ignored
                }
            }
            context.Result = new StatusCodeResult((int)HttpStatusCode.Unauthorized);
        }
    }
}