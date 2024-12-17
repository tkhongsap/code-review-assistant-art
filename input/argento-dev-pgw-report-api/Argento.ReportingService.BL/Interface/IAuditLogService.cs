using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Argento.ReportingService.DL.AuditLogs;

namespace Argento.ReportingService.BL.Interface
{
    public interface IAuditLogService
    {
        IList<AuditLogReadDto> GetAll();
        AuditLogReadDto Get(Guid id);
        Task SaveAuditLog(IEnumerable<AuditLogReadDto> auditLogs);
    }
}
