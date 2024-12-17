using Argento.ReportingService.Utility.Enums;

namespace Argento.ReportingService.Utility.Utils.Interface
{
    public interface IHtmlToImageUtil
    {
        byte[] GenerateImage(string html);
        byte[] GenerateImage(string html, HtmlToImageFormat imageFormat);
    }
}