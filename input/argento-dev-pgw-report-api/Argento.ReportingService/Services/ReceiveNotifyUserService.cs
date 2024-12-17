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
    internal class ReceiveNotifyUserService : IReceiveNotifyUserService
    {
        private readonly ILogger<ReceiveNotifyUserService> _logger;
        private readonly AppSettings _appSettings;
        private readonly IUnitOfWorkReportingServiceDB _unitOfWork;

        private string topic = "";
        private string groupId = "";

        public ReceiveNotifyUserService(
            ILogger<ReceiveNotifyUserService> logger,
            IOptions<AppSettings> appSettings, 
            IUnitOfWorkReportingServiceDB unitOfWork)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
            _unitOfWork = unitOfWork;

            topic = _appSettings.MyConfig.SendNotifyUserTopic;
            groupId = $"{topic}_group1";
        }

        public async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReceiveNotifyUserService.DoWork init");

            var config = new ConsumerConfig
            {
                GroupId = groupId,
                BootstrapServers = _appSettings.MyConfig.KafkaUrl,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("ReceiveNotifyUserService.DoWork running");

                try
                {
                    using (var consumerBuilder = new ConsumerBuilder
                    <Ignore, string>(config).Build())
                    {
                        consumerBuilder.Subscribe(topic);

                        var cancelToken = new CancellationTokenSource();

                        try
                        {
                            while (true)
                            {
                                var consumer = consumerBuilder.Consume(cancelToken.Token);
                                var consumerResult = consumer.Message.Value;

                                if (!string.IsNullOrEmpty(consumerResult))
                                {
                                    _logger.LogInformation($"ReceiveNotifyUserService.DoWork consumer: {consumerResult}");

                                    var dto = JsonSerializer.Deserialize<SendNotifyUserDto>(consumerResult);

                                    var userRepo = _unitOfWork.GetRepository<UserEntity>();
                                    var find = await userRepo.GetAll(false)
                                        .Where(x => x.Id == Guid.Parse(dto.Id) && !x.IsDeleted)
                                        .FirstOrDefaultAsync();

                                    if (find is null)
                                    {
                                        var user = new UserEntity
                                        {
                                            Id = Guid.Parse(dto.Id),
                                            Firstname = dto.firstname,
                                            Lastname = dto.lastname,
                                            PhoneNumber = dto.phoneNumber,
                                            Email = dto.email,
                                            MerchantId = dto.MerchantId.Length > 0 ? Guid.Parse(dto.MerchantId) : Guid.Empty,
                                            MerchantName = dto.MerchantName,
                                        };

                                        await userRepo.AddAsync(dto.MerchantId, user);
                                        await userRepo.UnitOfWork.SaveChangesAsync();
                                    }
                                    else
                                    {
                                        find.Firstname = dto.firstname;
                                        find.Lastname = dto.lastname;
                                        find.PhoneNumber = dto.phoneNumber;
                                        find.Email = dto.email;
                                        find.MerchantId = dto.MerchantId.Length > 0 ? Guid.Parse(dto.MerchantId) : Guid.Empty;
                                        find.MerchantName = dto.MerchantName;

                                        await userRepo.UpdateAsync(dto.MerchantId, find);
                                        await userRepo.UnitOfWork.SaveChangesAsync();
                                    }

                                    _logger.LogInformation($"ReceiveNotifyUserService.DoWork save to db complete");
                                }

                                consumerBuilder.Commit(consumer);
                            }
                        }
                        catch (Exception ex1)
                        {
                            _logger.LogError($"[ERROR] ReceiveNotifyUserService.DoWork message3: {ex1.Message}");

                            consumerBuilder.Close();

                            await Task.Delay(TimeSpan.FromSeconds(10));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[ERROR] ReceiveNotifyUserService.DoWork message: {ex.Message}");
                }
            }
        }
    }
}
