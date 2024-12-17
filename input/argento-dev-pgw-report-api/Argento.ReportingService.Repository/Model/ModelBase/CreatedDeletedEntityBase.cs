using Arcadia.Repository.EFCore.Entities;
using Argento.ReportingService.Utility;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Argento.ReportingService.Repository.Model
{
    public abstract class CreatedDeletedEntityBase : CreatedEntityBase, IDeletedBy
    {
        [DefaultValue(false)]
        public bool IsDeleted { get; set; }
        [StringLength(ArcadiaConstants.Database.IdMaxLength)]
        public Guid? DeletedByUserId { get; set; }
        public DateTime? DeletedTimestamp { get; set; }
    }

}
