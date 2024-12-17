using Argento.ReportingService.Utility.Utils.Interface;
using Arcadia.Extensions.DependencyInjection.Attributes;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace Argento.ReportingService.Utility.Utils
{
    [RegisterType(typeof(IImageEmailUtil))]
    public class ImageEmailUtil : IImageEmailUtil
    {
        private string smtpHost;
        private string smtpPort;
        private string smtpUserId;
        private string smtpUserPwd;
        private IHtmlToImageUtil htmlToImageUtil;

        public ImageEmailUtil(IOptions<AppSettings> appSettingsOptions)
        {
            AppSettings appSettings = appSettingsOptions.Value;

            smtpHost = appSettings.EmailConfig.SmtpHost;
            smtpPort = appSettings.EmailConfig.SmtpPort;
            smtpUserId = appSettings.EmailConfig.SmtpUserId;
            smtpUserPwd = appSettings.EmailConfig.SmtpUserPassword;
        }

        public void SendEmailImage(string sendTo, string subject, string htmlBody)
        {
            SendInternal(sendTo, subject, htmlBody, smtpHost, smtpPort, smtpUserId, smtpUserPwd);
        }

        private void SendInternal(
            string toId,
            string msgSub,
            string htmlBody,
            string smtpHost,
            string smtPort,
            string smtpUserId,
            string smtpUserPwd,
            string authUserName = null,
            string authUserPwd = null,
            string bccToSuperAdmin = ArcadiaConstants.No,
            string fromName = ""
        )
        {
            byte[] imageBytes = htmlToImageUtil.GenerateImage(htmlBody);
            //FileUtil fileutil = new FileUtil();
            //fileutil.WriteAllBytes(@"C:\testimg.jpg", imageBytes);
            LinkedResource template = new LinkedResource(new MemoryStream(imageBytes))
            {
                ContentId = "templateId",
                ContentType = new ContentType(MediaTypeNames.Image.Jpeg)
            };

            // Write image on the HTML formatting
            AlternateView alternateView = AlternateView.CreateAlternateViewFromString(
                  "<html><body><img src='cid:templateId'/>" +
                  "<br></body></html>",
                  null, MediaTypeNames.Text.Html);
            alternateView.LinkedResources.Add(template);

            var mail = new MailMessage();

            if (!string.IsNullOrWhiteSpace(fromName))
                mail.From = new MailAddress(smtpUserId, "" + fromName);
            else
                mail.From = new MailAddress(smtpUserId);

            mail.To.Add(toId);

            if (bccToSuperAdmin == ArcadiaConstants.Yes)
            {
                string mailSuperAdmin = smtpUserId;
                mail.Bcc.Add(mailSuperAdmin);
            }

            mail.Subject = msgSub;

            // Append AlternateView to the body of the mail
            mail.AlternateViews.Add(alternateView);

            var smtp = new SmtpClient(smtpHost);
            smtp.Port = Convert.ToInt16(smtPort);

            if (string.IsNullOrWhiteSpace(authUserName) || string.IsNullOrWhiteSpace(authUserPwd))
                smtp.Credentials = new NetworkCredential(smtpUserId, smtpUserPwd);
            else
                smtp.Credentials = new NetworkCredential(authUserName, authUserPwd);

            smtp.Send(mail);
        }
    }
}