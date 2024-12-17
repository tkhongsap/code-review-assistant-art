using System.Collections.Generic;

namespace Argento.ReportingService.Utility
{
    public class AppSettings
    {
        public string EnvironmentName { get; set; }
        public Connectionstrings ConnectionStrings { get; set; }
        public Logging Logging { get; set; }
        public string AllowedHosts { get; set; }
        public string EncryptionKey { get; set; }
        public string EncryptionSalt { get; set; }
        public EmailConfig EmailConfig { get; set; }
        public string JsonSerializerOptionsDateTimeFormat { get; set; }
        public MyConfig MyConfig { get; set; }
        public int MaxPageSize { get; set; }
        public MailReportConfig MailReportConfig { get; set; }
        public List<Role> Role { get; set; }
    }

    public class Connectionstrings
    {
        public string DefaultConnection { get; set; }
    }

    public class Logging
    {
        public Loglevel LogLevel { get; set; }
    }

    public class Loglevel
    {
        public string Default { get; set; }
    }

    public class MyConfig
    {
        public string KafkaUrl { get; set; }
        public string SendNotifyUserTopic { get; set; }
        public string SendNotifyUserMenuRoleTopic { get; set; }
        public string MerchantIntegrationTopic { get; set; }
        public string MerchantServiceTypeIntegrationTopic { get; set; }
        public string AccountIntegrationTopic { get; set; }
        public string UpdateCallbackUrlTopic { get; set; }
    }

    public class Role
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public List<Eamils> Emails { get; set; }
    }
    public class MailReportConfig
    {
        public string Host { get; set; }
        public string From { get; set; }
        public int Port { get; set; }
    }

    public class Eamils
    {
        public string Email { get; set; }
    }
}
