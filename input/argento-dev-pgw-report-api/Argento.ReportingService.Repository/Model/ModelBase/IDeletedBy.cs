using Argento.ReportingService.Utility;
using System;
using System.ComponentModel.DataAnnotations;

namespace Argento.ReportingService.Repository.Model
{
    public interface IDeletedBy
    {
        bool IsDeleted { get; set; }
        /// <summary>
        /// User Id
        /// </summary>
        Guid? DeletedByUserId { get; set; }
        DateTime? DeletedTimestamp { get; set; }
    }
}
