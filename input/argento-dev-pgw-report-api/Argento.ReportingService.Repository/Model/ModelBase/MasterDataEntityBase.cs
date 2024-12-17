using System;
using System.ComponentModel;

namespace Argento.ReportingService.Repository.Model
{
    public abstract class MasterDataEntityBase : CreatedEntityBase, ILastModifiedBy, IDeletedBy
    {
        public Guid? LastModifiedByUserId { get; set; }
        public DateTime? LastModifiedTimestamp { get; set; }
        public Guid? DeletedByUserId { get; set; }
        public DateTime? DeletedTimestamp { get; set; }
        [DefaultValue(false)]
        public bool IsDeleted { get; set; }
    }

}
