using Argento.ReportingService.Utility.Utils.Interface;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using Arcadia.Extensions.DependencyInjection.Attributes;

namespace Argento.ReportingService.Utility.Utils
{
    [RegisterType(typeof(IEmailUtil))]
    public class EmailUtil : IEmailUtil
    {

        private string smtpHost;
        private string smtpPort;
        private string smtpUserId;
        private string smtpUserPwd;

        public EmailUtil(IOptions<AppSettings> appSettingsOptions)
        {
            AppSettings appSettings = appSettingsOptions.Value;

            smtpHost = appSettings.EmailConfig.SmtpHost;
            smtpPort = appSettings.EmailConfig.SmtpPort;
            smtpUserId = appSettings.EmailConfig.SmtpUserId;
            smtpUserPwd = appSettings.EmailConfig.SmtpUserPassword;
        }

        public void Send(string sendTo, string subject, string message)
        {
            SendInternal(sendTo, subject, message, smtpHost, smtpPort, smtpUserId, smtpUserPwd);
        }

        private static void SendInternal(
            string toId,
            string msgSub,
            string msgBody,
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
            var mail = new MailMessage();
            mail.From = string.IsNullOrWhiteSpace(fromName) ? new MailAddress(smtpUserId) : new MailAddress(smtpUserId, fromName);
            mail.To.Add(toId);

            if (bccToSuperAdmin == ArcadiaConstants.Yes)
            {
                mail.Bcc.Add(smtpUserId);
            }

            mail.Subject = msgSub;
            mail.Body = msgBody;
            mail.BodyEncoding = Encoding.UTF8;
            mail.IsBodyHtml = true;

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