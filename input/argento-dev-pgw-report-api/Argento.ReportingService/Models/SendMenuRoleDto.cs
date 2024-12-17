using System.Collections.Generic;
using System;

namespace Argento.ReportingService.Models
{
    public class SendMenuRoleDto
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
        public List<RoleMenuNotify> RoleMenus { get; set; }
        public List<RoleSubMenuNotify> RoleSubMenus { get; set; }
    }
}
