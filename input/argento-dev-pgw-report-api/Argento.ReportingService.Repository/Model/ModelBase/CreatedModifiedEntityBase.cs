using Argento.ReportingService.Utility;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Argento.ReportingService.Repository.Model
{
    public abstract class CreatedModifiedEntityBase : CreatedEntityBase, ILastModifiedBy
    {
        public Guid? LastModifiedByUserId { get; set; }
        public DateTime? LastModifiedTimestamp { get; set; }
    }

}
