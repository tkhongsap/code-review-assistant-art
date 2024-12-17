namespace Argento.ReportingService.Utility
{
    public class EmailConfig
    {
        public string SmtpHost { get; set; }
        public string SmtpPort { get; set; }
        public string SmtpUserId { get; set; }
        public string SmtpUserPassword { get; set; }
        public bool EnableSendingEmailFlag { get; set; }
    }
}