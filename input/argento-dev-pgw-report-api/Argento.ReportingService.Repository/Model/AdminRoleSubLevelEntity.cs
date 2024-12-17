using Arcadia.Repository.EFCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.Repository.Model
{
    public class AdminRoleSubLevelEntity : EntityBase
    {
        public Guid Id { get; set; }
        public Guid AdminSubLevelId { get; set; }
        public Guid RoleId { get; set; }
        public bool IsActive { get; set; }
        public string Action { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
