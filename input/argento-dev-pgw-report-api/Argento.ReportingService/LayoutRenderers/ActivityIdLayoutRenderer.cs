using Argento.ReportingService.Utility;
using NLog;
using NLog.LayoutRenderers;
using NLog.Web.LayoutRenderers;
using System.Text;

namespace Argento.ReportingService.LayoutRenderers
{
    [LayoutRenderer("activityId")]
    public class ActivityIdLayoutRenderer : AspNetLayoutRendererBase
    {
        protected override void DoAppend(StringBuilder builder, LogEventInfo logEvent)
        {
            object activityId = null;
            bool? hasActivityId = HttpContextAccessor?.HttpContext?.Items?.TryGetValue(ArcadiaConstants.RequestScopeKeys.ActivityId, out activityId);
            if (!hasActivityId.HasValue || hasActivityId.Value == false)
            {
                builder.Append(string.Empty);
            }
            else
            {
                builder.Append(activityId);
            }
        }
    }
}