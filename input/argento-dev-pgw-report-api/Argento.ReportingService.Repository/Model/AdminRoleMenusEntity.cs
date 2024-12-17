using Arcadia.Repository.EFCore.Entities;
using System;

namespace Argento.ReportingService.Repository.Model
{
    public class AdminRoleMenusEntity : EntityBase
    {
        public Guid MenuId { get; set; }
        public Guid RoleId { get; set; }
        public string Action { get; set; }
    }
}
