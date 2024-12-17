using Arcadia.Repository.EFCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.Repository.Model
{
    public class AdminRolesEntity : EntityBase
    {
        public Guid Id { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
    }
}
