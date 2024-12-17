using Arcadia.Repository.EFCore.Entities;
using Argento.ReportingService.Utility;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Argento.ReportingService.Repository.Model
{
    public abstract class CreatedEntityBase : EntityBase, IId, ICreatedBy
    {
        public Guid Id { get; set; }
        public Guid? CreatedByUserId { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? CreatedTimestamp { get; set; }
    }

}
