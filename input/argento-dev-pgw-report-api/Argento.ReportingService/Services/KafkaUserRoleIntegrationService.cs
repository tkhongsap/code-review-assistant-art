using Argento.ReportingService.Models;
using Argento.ReportingService.Repository;
using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Utility;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Argento.ReportingService.Services
{
    internal class KafkaUserRoleIntegrationService : IScopedUserRoleIntegrationService
    {
        private readonly ILogger _logger;
        private readonly AppSettings _appSettings;
        private readonly IUnitOfWorkReportingServiceDB _unitOfWork;

        private string topic = "";
        private string groupId = "";

        public KafkaUserRoleIntegrationService(
            ILogger<KafkaUserRoleIntegrationService> logger,
            IOptions<AppSettings> appSettings,
            IUnitOfWorkReportingServiceDB unitOfWork)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
            _unitOfWork = unitOfWork;

            topic = _appSettings.MyConfig.SendNotifyUserMenuRoleTopic;
            groupId = $"{topic}_group3";
        }

        public async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KafkaUserRoleIntegrationService.DoWork init");

            var config = new ConsumerConfig
            {
                GroupId = groupId,
                BootstrapServers = _appSettings.MyConfig.KafkaUrl,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };


            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("KafkaUserRoleIntegrationService.DoWork running");

                try
                {
                    using (var consumerBuilder = new ConsumerBuilder
                    <Ignore, string>(config).Build())
                    {
                        consumerBuilder.Subscribe(topic);

                        var cancelToken = new CancellationTokenSource();

                        string reqId = "";

                        try
                        {

                            while (true)
                            {

                                var consumer = consumerBuilder.Consume(cancelToken.Token);
                                var consumerResult = consumer.Message.Value;

                                //_logger.LogInformation($"ScopedKafkaService.DoWork 5");

                                if (!string.IsNullOrEmpty(consumerResult))
                                {
                                    reqId = Guid.NewGuid().ToString();

                                    _logger.LogInformation($"KafkaUserRoleIntegrationService.DoWork consumer: {consumerResult} reqId: {reqId}");

                                    var dto = JsonSerializer.Deserialize<SendMenuRoleDto>(consumerResult);

                                    var adminRepo = _unitOfWork.GetRepository<AdminRolesEntity>();
                                    var adminEntity = await adminRepo.GetAll().Where(x => x.Id == dto.RoleId).FirstOrDefaultAsync();
                                    var isCreate = false;
                                    if (adminEntity == null)
                                    {
                                        _logger.LogDebug($"KafkaUserRoleIntegrationService.DoWork Create New Role reqId: {reqId}");

                                        await adminRepo.AddAsync(Guid.NewGuid(), new AdminRolesEntity
                                        {
                                            Id = dto.RoleId,
                                            IsActive = dto.IsActive,
                                            RoleName = dto.RoleName,
                                        });

                                        await adminRepo.UnitOfWork.SaveChangesAsync();
                                        isCreate = true;
                                    }
                                    else if (adminEntity.RoleName != dto.RoleName)
                                    {
                                        _logger.LogDebug($"KafkaUserRoleIntegrationService.DoWork Update Role reqId: {reqId} , RoleId : {dto.RoleId}");

                                        adminEntity.RoleName = dto.RoleName;
                                        await adminRepo.UpdateAsync(Guid.NewGuid(), adminEntity);
                                        await adminRepo.UnitOfWork.SaveChangesAsync();
                                    }

                                    var roleMenuRepo = _unitOfWork.GetRepository<AdminRoleMenusEntity>();
                                    var roleSubMenuRepo = _unitOfWork.GetRepository<AdminRoleSubLevelEntity>();

                                    // Remove when update the existing role
                                    if (!isCreate)
                                    {
                                        _logger.LogDebug($"KafkaUserRoleIntegrationService.DoWork Remove Role Menu reqId: {reqId} , RoleId : {dto.RoleId}");

                                        var roleMenus = await roleMenuRepo.GetAll().Where(x => x.RoleId == adminEntity.Id).ToArrayAsync();
                                        await roleMenuRepo.DeleteRangeAsync(Guid.NewGuid(), roleMenus);

                                        _logger.LogDebug($"KafkaUserRoleIntegrationService.DoWork Remove Role Sub Menu reqId: {reqId} , RoleId : {dto.RoleId}");

                                        var roleSubMenus = await roleSubMenuRepo.GetAll().Where(x => x.RoleId == adminEntity.Id).ToArrayAsync();
                                        await roleSubMenuRepo.DeleteRangeAsync(Guid.NewGuid(), roleSubMenus);
                                    }

                                    var roleMenuEntity = dto.RoleMenus.Select(x => new AdminRoleMenusEntity
                                    {
                                        Action = x.Action,
                                        MenuId = x.Id,
                                        RoleId = x.RoleId,
                                    }).ToArray();

                                    _logger.LogDebug($"KafkaUserRoleIntegrationService.DoWork Add Role Menus reqId: {reqId} , RoleId : {dto.RoleId}");

                                    await roleMenuRepo.AddRangeAsync(Guid.NewGuid(), roleMenuEntity);
                                    await roleMenuRepo.UnitOfWork.SaveChangesAsync();

                                    var roleSubMenuEntity = dto.RoleSubMenus.Select(x => new AdminRoleSubLevelEntity
                                    {
                                        Action = x.Action,
                                        AdminSubLevelId = x.AdminSubLevelId,
                                        RoleId = x.RoleId,
                                        CreatedAt = DateTime.UtcNow,
                                        Id = x.Id,
                                        IsActive = x.IsActive,
                                    }).ToArray();

                                    _logger.LogDebug($"KafkaUserRoleIntegrationService.DoWork Add Role Sub Menus reqId: {reqId} , RoleId : {dto.RoleId}");

                                    await roleSubMenuRepo.AddRangeAsync(Guid.NewGuid(), roleSubMenuEntity);
                                    await roleSubMenuRepo.UnitOfWork.SaveChangesAsync();

                                    _logger.LogInformation($"KafkaUserRoleIntegrationService.DoWork save to db complete reqId: {reqId}");
                                }

                                consumerBuilder.Commit(consumer);
                            }
                        }
                        catch (Exception ex1)
                        {
                            _logger.LogError($"[ERROR] KafkaUserRoleIntegrationService.DoWork message3: {ex1.Message} reqId: {reqId}");

                            consumerBuilder.Close();

                            await Task.Delay(TimeSpan.FromSeconds(10));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[ERROR] KafkaUserRoleIntegrationService.DoWork message: {ex.Message}");
                }
            }
        }
    }
}
