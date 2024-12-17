using Argento.ReportingService.Models;
using Argento.ReportingService.Repository;
using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Utility;
using Argento.ReportingService.Utility.Utils;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Argento.ReportingService.Services
{
    internal class KafkaAccountIntegrationService : IScopedAccountIntegrationService
    {
        private readonly ILogger _logger;
        private readonly AppSettings _appSettings;
        private readonly IUnitOfWorkReportingServiceDB _unitOfWork;

        private string topic = "";
        private string groupId = "";

        public KafkaAccountIntegrationService(
            ILogger<KafkaAccountIntegrationService> logger,
            IOptions<AppSettings> appSettings,
            IUnitOfWorkReportingServiceDB unitOfWork)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
            _unitOfWork = unitOfWork;

            topic = _appSettings.MyConfig.AccountIntegrationTopic;
            groupId = $"{topic}_group3";
        }

        public async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KafkaAccountIntegrationService.DoWork init");

            var config = new ConsumerConfig
            {
                GroupId = groupId,
                BootstrapServers = _appSettings.MyConfig.KafkaUrl,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            {
                _logger.LogInformation("KafkaAccountIntegrationService.DoWork running");

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
                                    _logger.LogInformation($"KafkaAccountIntegrationService.DoWork consumer: {consumerResult}");

                                    var dto = JsonSerializer.Deserialize<KafkaAccountRequest>(consumerResult);

                                    var accountRepository = _unitOfWork.GetRepository<AccountEntity>();
                                    var account = await accountRepository.GetAll(false)
                                        .Where(x => x.Id == dto.Id)
                                        .FirstOrDefaultAsync();

                                    if (account == null)
                                    {
                                        // insert to merchant
                                        var accountItem = new AccountEntity
                                        {
                                            Id = dto.Id,
                                            MerchantId = dto.MerchantId,
                                            BankCode = dto.BankCode,
                                            BankName = dto.BankName,
                                            BankBranch = dto.BankBranch,
                                            AccountNo = dto.AccountNo,
                                            AccountName = dto.AccountName,
                                            AccountTypeId = dto.AccountTypeId,
                                            AccountTypeName = dto.AccountTypeName,
                                            IsPrimary = dto.IsPrimary,
                                            IsActive = dto.IsActive,
                                            LastModifiedByUserId = dto.LastModifiedByUserId,
                                            LastModifiedTimestamp = dto.LastModifiedTimestamp,
                                            DeletedByUserId = dto.DeletedByUserId,
                                            DeletedTimestamp = dto.DeletedTimestamp,
                                            IsDeleted = dto.IsDeleted,
                                            CreatedByUserId = dto.CreatedByUserId,
                                            CreatedTimestamp = dto.CreatedTimestamp,
                                        };

                                        var plainTextBytes = Encoding.UTF8.GetBytes(dto.MerchantId.ToString());

                                        if (dto.IsPrimary)
                                        {
                                            var listAccount = await accountRepository.GetAll(true).Where(a => a.MerchantId == dto.MerchantId).ToListAsync();
                                            if (listAccount.Any())
                                            {
                                                foreach (var item in listAccount)
                                                {
                                                    item.IsPrimary = false;
                                                }
                                                await accountRepository.UpdateRangeAsync(listAccount);
                                            }
                                        }


                                        await accountRepository.AddAsync(dto.CreatedByUserId, accountItem);
                                        await accountRepository.UnitOfWork.SaveChangesAsync();
                                    }
                                    else
                                    {
                                        // update
                                        account.BankCode = dto.BankCode;
                                        account.BankName = dto.BankName;
                                        account.BankBranch = dto.BankBranch;
                                        account.AccountNo = dto.AccountNo;
                                        account.AccountName = dto.AccountName;
                                        account.AccountTypeId = dto.AccountTypeId;
                                        account.AccountTypeName = dto.AccountTypeName;
                                        account.IsPrimary = dto.IsPrimary;
                                        account.IsActive = dto.IsActive;
                                        account.LastModifiedByUserId = dto.LastModifiedByUserId;
                                        account.LastModifiedTimestamp = dto.LastModifiedTimestamp;
                                        account.DeletedByUserId = dto.DeletedByUserId;
                                        account.DeletedTimestamp = dto.DeletedTimestamp;
                                        account.IsDeleted = dto.IsDeleted;
                                        account.CreatedByUserId = dto.CreatedByUserId;
                                        account.CreatedTimestamp = dto.CreatedTimestamp;

                                        if (dto.IsPrimary)
                                        {
                                            var listAccount = await accountRepository.GetAll(true).Where(a => a.MerchantId == dto.MerchantId).ToListAsync();
                                            if (listAccount.Any())
                                            {
                                                foreach (var accountItem in listAccount)
                                                {
                                                    accountItem.IsPrimary = false;
                                                }
                                                await accountRepository.UpdateRangeAsync(listAccount);
                                            }
                                        }

                                        Guid userId = dto.LastModifiedByUserId != null ? dto.LastModifiedByUserId.Value : dto.CreatedByUserId.Value;
                                        await accountRepository.UpdateAsync(userId, account);
                                        await accountRepository.UnitOfWork.SaveChangesAsync();
                                    }

                                    _logger.LogInformation($"KafkaAccountIntegrationService.DoWork save to db complete");
                                }

                                consumerBuilder.Commit(consumer);
                            }
                        }
                        catch (Exception ex1)
                        {
                            _logger.LogError($"[ERROR] KafkaAccountIntegrationService.DoWork message3: {ex1.Message}");

                            consumerBuilder.Close();

                            await Task.Delay(TimeSpan.FromSeconds(10));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[ERROR] KafkaAccountIntegrationService.DoWork message: {ex.Message}");
                }
            }
        }
    }
}
