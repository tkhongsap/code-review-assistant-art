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
    internal class KafkaMerchantIntegrationService : IScopedMerchantIntegrationService
    {
        private readonly ILogger _logger;
        private readonly AppSettings _appSettings;
        private readonly IUnitOfWorkReportingServiceDB _unitOfWork;

        private string topic = "";
        private string groupId = "";

        public KafkaMerchantIntegrationService(
            ILogger<KafkaMerchantIntegrationService> logger,
            IOptions<AppSettings> appSettings,
            IUnitOfWorkReportingServiceDB unitOfWork)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
            _unitOfWork = unitOfWork;

            topic = _appSettings.MyConfig.MerchantIntegrationTopic;
            groupId = $"{topic}_group3";
        }

        public async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KafkaMerchantIntegrationService.DoWork init");

            var config = new ConsumerConfig
            {
                GroupId = groupId,
                BootstrapServers = _appSettings.MyConfig.KafkaUrl,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("KafkaMerchantIntegrationService.DoWork running");

                    try
                    {
                        using (var consumerBuilder = new ConsumerBuilder
                        <Ignore, string>(config).Build())
                        {
                            //_logger.LogInformation($"ScopedKafkaService.DoWork 1");

                            consumerBuilder.Subscribe(topic);

                            //_logger.LogInformation($"ScopedKafkaService.DoWork 2");

                            var cancelToken = new CancellationTokenSource();
                            string reqId = Guid.NewGuid().ToString();

                            try
                            {
                                while (!stoppingToken.IsCancellationRequested)
                                {
                                    var consumer = consumerBuilder.Consume(cancelToken.Token);
                                    var consumerResult = consumer.Message.Value;

                                    if (!string.IsNullOrEmpty(consumerResult))
                                    {
                                        _logger.LogInformation($"KafkaMerchantIntegrationService.DoWork consumer: {consumerResult}");

                                        var dto = JsonSerializer.Deserialize<KafkaMerchantIntegrationRequest>(consumerResult);

                                        _logger.LogInformation($"KafkaMerchantIntegrationService.DoWork MerchantId: {dto.MerchantId}, ReqId: {reqId}");

                                        var merchantRepo = _unitOfWork.GetRepository<MerchantEntity>();
                                        var merchant = await merchantRepo.GetAll(false)
                                            .Where(x => x.Id == dto.MerchantId && !x.IsDeleted)
                                            .FirstOrDefaultAsync();
                                        var mainMerchant = await merchantRepo.GetAll(false).Where(x => x.Id == dto.MainBranchId).FirstOrDefaultAsync();
                                        if (merchant == null)
                                        {
                                            // insert to merchant
                                            _logger.LogInformation($"KafkaMerchantIntegrationService.DoWork MerchantId: {dto.MerchantId} is not exists [INSERT], ReqId: {reqId}");

                                            var merchantItem = new MerchantEntity
                                            {
                                                Id = dto.MerchantId,
                                                MerchantName = dto.MerchantName,
                                                MerchantCode = dto.MerchantCode,
                                                CallbackUrl = dto.CallbackUrl,
                                                MainBranchId = dto.MainBranchId,
                                                MainBranchName = dto.MainBranchName,
                                                PaymentChannels = dto.PaymentChannels,
                                                Services = dto.Services,
                                                PaymentTerm = dto.PaymentTerm,
                                                SapCustomerId = dto.SapCustomerId,
                                                IsCompany = dto.IsCompany,
                                                Email = dto.Email,
                                                Phone = dto.Phone,
                                                MerchantCategoryId = dto.MerchantCategoryId,
                                                MerchantCategoryName = dto.MerchantCategoryName,
                                                CustomerGroup = dto.CustomerGroup,
                                                MdrRate = mainMerchant != null ? mainMerchant.MdrRate : null,
                                                MerchantServiceType = dto.MerchantServiceType
                                            };

                                            var plainTextBytes = Encoding.UTF8.GetBytes(dto.MerchantId.ToString());

                                            merchantItem.MerchantKey = Convert.ToBase64String(plainTextBytes);
                                            merchantItem.SecretKey = RandomString.Generate(12);

                                            await merchantRepo.AddAsync(dto.MerchantId, merchantItem);
                                            await merchantRepo.UnitOfWork.SaveChangesAsync();

                                            // insert to account
                                            var accountRepo = _unitOfWork.GetRepository<AccountEntity>();
                                            var accountItem = new AccountEntity
                                            {
                                                Id = dto.AccountId,
                                                MerchantId = dto.MerchantId,
                                                BankCode = dto.BankCode,
                                                BankName = dto.BankName,
                                                BankBranch = dto.BankBranch,
                                                AccountName = dto.AccountName,
                                                AccountNo = dto.AccountNo,
                                                AccountTypeId = dto.AccountTypeId,
                                                AccountTypeName = dto.AccountTypeName,
                                                IsActive = false,
                                                IsPrimary = true,
                                            };

                                            await accountRepo.AddAsync(dto.MerchantId, accountItem);
                                            await accountRepo.UnitOfWork.SaveChangesAsync();
                                        }
                                        else
                                        {
                                            _logger.LogInformation($"KafkaMerchantIntegrationService.DoWork MerchantId: {dto.MerchantId} is exists [UPDATE], ReqId: {reqId}");

                                            // update
                                            merchant.CallbackUrl = dto.CallbackUrl;
                                            merchant.MerchantName = dto.MerchantName;
                                            merchant.MainBranchId = dto.MainBranchId;
                                            merchant.MainBranchName = dto.MainBranchName;
                                            merchant.PaymentChannels = dto.PaymentChannels;
                                            merchant.Services = dto.Services;
                                            merchant.PaymentTerm = dto.PaymentTerm;
                                            merchant.SapCustomerId = dto.SapCustomerId;
                                            merchant.IsCompany = dto.IsCompany;
                                            merchant.Email = dto.Email;
                                            merchant.Phone = dto.Phone;
                                            merchant.MerchantCode = dto.MerchantCode;
                                            merchant.MerchantCategoryId = dto.MerchantCategoryId;
                                            merchant.MerchantCategoryName = dto.MerchantCategoryName;
                                            merchant.CustomerGroup = dto.CustomerGroup;

                                            var subMerchant = await merchantRepo.GetAll(false)
                                            .Where(x => x.MainBranchId == dto.MerchantId && !x.IsDeleted).ToListAsync();

                                            foreach (var item in subMerchant)
                                            {
                                                item.PaymentTerm = dto.PaymentTerm;
                                                await merchantRepo.UpdateAsync(item.Id, item);
                                            }

                                            await merchantRepo.UpdateAsync(merchant.Id, merchant);
                                            await merchantRepo.UnitOfWork.SaveChangesAsync();
                                        }

                                        _logger.LogInformation($"KafkaMerchantIntegrationService.DoWork save to db complete, ReqId: {reqId}");
                                    }

                                    consumerBuilder.Commit(consumer);
                                }
                            }
                            catch (Exception ex1)
                            {
                                _logger.LogError($"[ERROR] KafkaMerchantIntegrationService.DoWork message3: {ex1.Message}, ReqId: {reqId}");

                                // show inner exception if exists
                                if (ex1.InnerException != null)
                                {
                                    _logger.LogError($"[ERROR] KafkaMerchantIntegrationService.DoWork inner exception message: {ex1.InnerException.Message}, ReqId: {reqId}");
                                }

                                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[ERROR] KafkaMerchantIntegrationService.DoWork message: {ex.Message}");
                    }
                }
            }
        }
    }
}