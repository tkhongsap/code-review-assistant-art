using Argento.ReportingService.Controllers.Internal;
using Argento.ReportingService.DL;
using Argento.ReportingService.DL.AuditLogs;
using Argento.ReportingService.Repository;
using Argento.ReportingService.Repository.Model;
using Arcadia.CrudController.AspNetCore;
using Arcadia.Repository.EFCore;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Argento.ReportingService.FilterAttributes;
using Arcadia.CrudController;

namespace Argento.ReportingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [CheckAuthentication]
    public class AuditLogsController : ReportingServiceDBCrudController<IUnitOfWorkReportingServiceDB, AuditLogEntity, AuditLogCreateDto, AuditLogReadDto, AuditLogUpdateDto, DeleteDto>
    {
        public AuditLogsController(IUnitOfWorkReportingServiceDB unitOfWork, RepositoryConfiguration repositoryConfiguration, IMapper mapper, IConfigurationProvider configurationProvider, CrudControllerConfiguration crudControllerConfiguration) : base(unitOfWork, repositoryConfiguration, mapper, configurationProvider, crudControllerConfiguration)
        {
        }
    }
}