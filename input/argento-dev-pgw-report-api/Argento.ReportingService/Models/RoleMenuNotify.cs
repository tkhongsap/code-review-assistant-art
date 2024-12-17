using System;

namespace Argento.ReportingService.Models
{
    public class RoleMenuNotify
    {
        public Guid Id { get; set; }
        public Guid RoleId { get; set; }
        public string Action { get; set; }
    }
}
