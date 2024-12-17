using System;
using System.ComponentModel.DataAnnotations;

namespace Argento.ReportingService.DL.AuditLogs
{
    public class AuditLogReadDto
    {
        public string Id { get; set; }
        public DateTime AuditDateTime { get; set; }
        [Required(ErrorMessage = "Username cannot be null or empty.")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Activity cannot be null or empty.")]
        public string Activity { get; set; }
        public string Details { get; set; }
        public string Page { get; set; }
        public string Source { get; set; }
        public DateTime? LastModifiedTimeStamp { get; set; }
    }
}
