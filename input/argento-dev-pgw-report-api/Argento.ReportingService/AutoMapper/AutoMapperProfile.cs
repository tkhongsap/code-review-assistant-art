using AutoMapper;
using Argento.ReportingService.DL.AuditLogs;
using Argento.ReportingService.Extensions;
using Argento.ReportingService.Repository.Model;

namespace Argento.ReportingService.AutoMapper
{
    public class AutoMapperProfile : Profile
    {

        public AutoMapperProfile()
        {
            this.CreateMapEntityClasses();

            CreateMap<AuditLogEntity, AuditLogCreateDto>().ReverseMap();
            CreateMap<AuditLogEntity, AuditLogReadDto>().ReverseMap();
            CreateMap<AuditLogEntity, AuditLogUpdateDto>().ReverseMap();
        }

    }
}
