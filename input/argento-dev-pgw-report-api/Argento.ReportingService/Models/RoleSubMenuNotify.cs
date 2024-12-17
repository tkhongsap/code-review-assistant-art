using System;

namespace Argento.ReportingService.Models
{
    public class RoleSubMenuNotify
    {
        public Guid Id { get; set; }
        public Guid AdminSubLevelId { get; set; }
        public Guid RoleId { get; set; }
        public bool IsActive { get; set; }
        public string Action { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
