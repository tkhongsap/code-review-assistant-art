using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Argento.ReportingService.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Argento.ReportingService.DL.Utils
{
    public class EmailAttachment
    {
        private readonly ILogger<EmailAttachment> _logger;
        private readonly AppSettings _appSettings;
        public EmailAttachment(ILogger<EmailAttachment> logger, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        public async Task SendEmailAsync(List<Eamils> email,string mailBody,  byte[] excelFile, string filename, string subject)
        {
            try
            {
                _logger.LogInformation($"[INFO] Process Send Email To Client: {JsonConvert.SerializeObject(email)}");
                MailMessage mailMessage = new MailMessage();
                _logger.LogInformation($"[INFO] Mail Address: {_appSettings.MailReportConfig.From}");
                MailAddress fromAddress = new MailAddress(_appSettings.MailReportConfig.From);
                mailMessage.From = fromAddress;

                foreach (var emailAddress in email)
                {
                    mailMessage.To.Add(emailAddress.Email);
                }

                Stream stream = new MemoryStream(excelFile);
                Attachment attachment = new Attachment(stream, filename);
                attachment.ContentType = new ContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.Attachments.Add(attachment);
                mailMessage.Subject = subject;
                mailMessage.IsBodyHtml = true;
                mailMessage.Body = mailBody;
                   
                _logger.LogInformation($"[INFO] Mail HOST: {_appSettings.MailReportConfig.Host}");
                var smtp = new SmtpClient(_appSettings.MailReportConfig.Host);
                _logger.LogInformation($"[INFO] Mail PORT: {_appSettings.MailReportConfig.Port}");
                smtp.Port = _appSettings.MailReportConfig.Port;
                _logger.LogInformation("[INFO] Sending email to client");
                await smtp.SendMailAsync(mailMessage);
                _logger.LogInformation("[INFO] Sended email success");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Error Message : {ex}");
                throw;
            }
        }
    }
}