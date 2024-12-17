using Argento.ReportingService.Utility.Enums;
using Argento.ReportingService.Utility.Utils.Interface;
using Arcadia.Extensions.DependencyInjection.Attributes;
using NReco.ImageGenerator;
using System;

namespace Argento.ReportingService.Utility.Utils
{
    [RegisterType(typeof(IHtmlToImageUtil))]
    public class HtmlToImageUtil : IHtmlToImageUtil
    {
        public byte[] GenerateImage(string html)
        {
            return GenerateImage(html, HtmlToImageFormat.Jpg);
        }

        public byte[] GenerateImage(string html, HtmlToImageFormat imageFormat)
        {
            //html = "<html>Hello World</html> ";

            var htmlToImageConv = new HtmlToImageConverter();
            if (imageFormat == HtmlToImageFormat.Jpg)
            {
                return htmlToImageConv.GenerateImage(html, ImageFormat.Jpeg);
            }
            else if (imageFormat == HtmlToImageFormat.Png)
            {
                return htmlToImageConv.GenerateImage(html, ImageFormat.Png);
            }
            else if (imageFormat == HtmlToImageFormat.Bmp)
            {
                return htmlToImageConv.GenerateImage(html, ImageFormat.Bmp);
            }
            throw new NotSupportedException($"{nameof(imageFormat)} = {imageFormat.ToString()}");
        }
    }
}