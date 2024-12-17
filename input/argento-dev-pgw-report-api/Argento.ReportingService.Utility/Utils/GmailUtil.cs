using Argento.ReportingService.Utility.Utils.Interface;
using Arcadia.Extensions.DependencyInjection.Attributes;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Argento.ReportingService.Utility.Utils
{
    [RegisterType(typeof(IGmailUtil))]
    public class GmailUtil : IGmailUtil
    {
        private string smtpHost;
        private string smtpPort;
        private string smtpUserId;
        private string smtpUserPwd;

        public GmailUtil(IOptions<AppSettings> appSettingsOptions)
        {
            AppSettings appSettings = appSettingsOptions.Value;

            smtpHost = appSettings.EmailConfig.SmtpHost;
            smtpPort = appSettings.EmailConfig.SmtpPort;
            smtpUserId = appSettings.EmailConfig.SmtpUserId;
            smtpUserPwd = appSettings.EmailConfig.SmtpUserPassword;
        }

        public void Send(string sendTo, string subject, string message)
        {
            SendGmail(sendTo, subject, message, smtpHost, smtpPort, smtpUserId, smtpUserPwd);
        }

        private static void SendGmail(string toId,
            string msgSub,
            string msgBody,
            string smtpHost,
            string smtpPort,
            string smtpUserId,
            string smtpUserPwd,
            string authUserName = null,
            string authUserPwd = null,
            string bccToSuperAdmin = ArcadiaConstants.Yes,
            string fromName = "")
        {

            var mail = new MailMessage();

            mail.From = !string.IsNullOrWhiteSpace(fromName) ? new MailAddress(smtpUserId, "" + fromName) : new MailAddress(smtpUserId);

            mail.To.Add(toId);

            if (bccToSuperAdmin == ArcadiaConstants.Yes)
            {
                string mailSuperAdmin = smtpUserId;
                mail.Bcc.Add(mailSuperAdmin);
            }

            mail.Subject = msgSub;
            mail.Body = msgBody;
            mail.BodyEncoding = Encoding.UTF8;
            mail.IsBodyHtml = true;
            mail.Priority = MailPriority.High; // By Gmail

            var smtp = new SmtpClient();
            smtp.UseDefaultCredentials = false; // By Gmail

            if (string.IsNullOrWhiteSpace(authUserName) || string.IsNullOrWhiteSpace(authUserPwd))
                smtp.Credentials = new NetworkCredential(smtpUserId, smtpUserPwd);

            smtp.Port = Convert.ToInt16(smtpPort);
            smtp.Host = smtpHost;
            smtp.EnableSsl = true; // By Gmail

            smtp.Send(mail);
        }

    }
}