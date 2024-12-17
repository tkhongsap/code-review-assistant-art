using Argento.ReportingService.DL.Users;
using Argento.ReportingService.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Argento.ReportingService.Controllers.Internal
{
    public abstract class ReportingServiceControllerBase : Controller
    {
        protected readonly string ActivityId;
        protected readonly RequestedUser RequestedUser;
        protected readonly Guid MerchantIdContext;
        protected readonly string UserId;
        protected ReportingServiceControllerBase(IHttpContextAccessor httpContextAccessor)
        {
            this.ActivityId = httpContextAccessor?.HttpContext?.Items[ArcadiaConstants.RequestScopeKeys.ActivityId]?.ToString();
            this.RequestedUser = (RequestedUser)httpContextAccessor?.HttpContext?.Items[ArcadiaConstants.RequestScopeKeys.RequestedByUser];
            this.MerchantIdContext = httpContextAccessor?.HttpContext?.Items[ArcadiaConstants.RequestScopeKeys.MerchantId] == null ?
                Guid.Parse("00000000-0000-0000-0000-000000000000") : Guid.Parse(httpContextAccessor?.HttpContext?.Items[ArcadiaConstants.RequestScopeKeys.MerchantId].ToString());
            this.UserId = httpContextAccessor?.HttpContext?.Items[ArcadiaConstants.RequestScopeKeys.UserId]?.ToString();
        }
    }
}
