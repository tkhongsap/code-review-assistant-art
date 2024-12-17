using Argento.ReportingService.Utility;
using System;
using System.ComponentModel.DataAnnotations;

namespace Argento.ReportingService.Repository.Model
{
    public interface ICreatedBy
    {
        /// <summary>
        /// User Id
        /// </summary>
        Guid? CreatedByUserId { get; set; }
        DateTime? CreatedTimestamp { get; set; }
    }
}
