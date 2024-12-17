using Argento.ReportingService.BL.Interface;
using Argento.ReportingService.DL.AuditLogs;
using Argento.ReportingService.Repository;
using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Repository.Repository;
using Argento.ReportingService.Utility;
using Arcadia.Extensions.DependencyInjection.Attributes;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Argento.ReportingService.BL.Service
{
    [RegisterType(typeof(IAuditLogService))]
    public class AuditLogService : IAuditLogService
    {

        private AppSettings appSettings;
        private IAuditLogRepository auditLogRepository;
        private IUnitOfWorkReportingServiceDB unitOfWork;
        private IConfigurationProvider configurationProvider;
        private IMapper mapper;

        public AuditLogService(IOptions<AppSettings> appSettings, IAuditLogRepository auditLogRepository, IUnitOfWorkReportingServiceDB unitOfWork, IConfigurationProvider configurationProvider, IMapper mapper)
        {
            this.auditLogRepository = auditLogRepository;
            this.unitOfWork = unitOfWork;
            this.configurationProvider = configurationProvider;
            this.mapper = mapper;
            this.appSettings = appSettings.Value;
        }

        public IList<AuditLogReadDto> GetAll()
        {
            return auditLogRepository.GetAll().ProjectTo<AuditLogReadDto>(configurationProvider).ToList();
        }

        public AuditLogReadDto Get(Guid id)
        {
            AuditLogEntity auditLogEntity = auditLogRepository.GetAll().FirstOrDefault(x => x.Id == id);
            return mapper.Map<AuditLogReadDto>(auditLogEntity);
        }

        public async Task SaveAuditLog(IEnumerable<AuditLogReadDto> auditLogs)
        {
            using (IDbContextTransaction trx = unitOfWork.BeginDbContextTransaction())
            {
                foreach (AuditLogReadDto auditLog in auditLogs)
                {
                    var e = new AuditLogEntity()
                    {
                        Id = Guid.NewGuid(), // always create a new record.
                        AuditDateTime = auditLog.AuditDateTime,
                        Username = auditLog.Username,
                        Details = auditLog.Details,
                        Activity = auditLog.Activity,
                        Page = auditLog.Page,
                        Source = auditLog.Source,
                    };
                    await auditLogRepository.AddAsync(1, e);
                }

                int result = await unitOfWork.SaveChangesAsync();
                if (result == 0)
                {
                    throw new InvalidOperationException("Fail to insert audit log into database.");
                }

                trx.Commit();
            }
        }

    }
}
