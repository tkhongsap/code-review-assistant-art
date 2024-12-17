using System;
using Arcadia.Repository.EFCore.Entities;

namespace Argento.ReportingService.Repository.Model
{
    public class AuditLogEntity : MasterDataEntityBase
    {
        public DateTime AuditDateTime { get; set; }
        public string Username { get; set; }
        public string Activity { get; set; }
        public string Details { get; set; }
        public string Page { get; set; }
        public string Source { get; set; }

    }
}
