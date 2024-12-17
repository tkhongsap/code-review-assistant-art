using Argento.ReportingService.Utility;
using System;
using System.ComponentModel.DataAnnotations;

namespace Argento.ReportingService.Repository.Model
{
    public interface IId
    {
        /// <summary>
        /// Primary Key
        /// </summary>
        Guid Id { get; set; }
    }
}
