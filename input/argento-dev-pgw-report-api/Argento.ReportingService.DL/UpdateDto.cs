using System;

namespace Argento.ReportingService.DL
{
    public abstract class UpdateDto
    {
        public string Id { get; set; }
        public DateTime? LastModifiedTimeStamp { get; set; }
    }
}