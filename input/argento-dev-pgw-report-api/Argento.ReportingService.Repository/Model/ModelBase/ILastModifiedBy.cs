using Argento.ReportingService.Utility;
using System;
using System.ComponentModel.DataAnnotations;

namespace Argento.ReportingService.Repository.Model
{
    public interface ILastModifiedBy
    {
        /// <summary>
        /// User Id
        /// </summary>
        Guid? LastModifiedByUserId { get; set; }
        DateTime? LastModifiedTimestamp { get; set; }
    }
}
