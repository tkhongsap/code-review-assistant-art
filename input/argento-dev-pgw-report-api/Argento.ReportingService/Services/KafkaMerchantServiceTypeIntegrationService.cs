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
    internal class KafkaMerchantServiceTypeIntegrationService : IScopedMerchantServiceTypeIntegrationService
    {
        private readonly ILogger _logger;
        private readonly AppSettings _appSettings;
        private readonly IUnitOfWorkReportingServiceDB _unitOfWork;

        private string topic = "";
        private string groupId = "";

        public KafkaMerchantServiceTypeIntegrationService(
            ILogger<KafkaMerchantServiceTypeIntegrationService> logger,
            IOptions<AppSettings> appSettings, 
            IUnitOfWorkReportingServiceDB unitOfWork)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
            _unitOfWork = unitOfWork;

            topic = _appSettings.MyConfig.MerchantServiceTypeIntegrationTopic;
            groupId = $"{topic}_group3";
        }

        public async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KafkaMerchantServiceTypeIntegrationService.DoWork init");

            var config = new ConsumerConfig
            {
                GroupId = groupId,
                BootstrapServers = _appSettings.MyConfig.KafkaUrl,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            //_logger.LogInformation($"KafkaMerchantServiceTypeIntegrationService.DoWork KafkaUrl: {_config.KafkaUrl}");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("KafkaMerchantServiceTypeIntegrationService.DoWork running");

                try
                {
                    using (var consumerBuilder = new ConsumerBuilder
                    <Ignore, string>(config).Build())
                    {
                        //_logger.LogInformation($"ScopedKafkaService.DoWork 1");

                        consumerBuilder.Subscribe(topic);

                        //_logger.LogInformation($"ScopedKafkaService.DoWork 2");

                        var cancelToken = new CancellationTokenSource();

                        string reqId = "";

                        try
                        {
                            //_logger.LogInformation($"ScopedKafkaService.DoWork 3");

                            while (true)
                            {
                                //_logger.LogInformation($"ScopedKafkaService.DoWork 4");

                                var consumer = consumerBuilder.Consume(cancelToken.Token);
                                var consumerResult = consumer.Message.Value;

                                //_logger.LogInformation($"ScopedKafkaService.DoWork 5");

                                if (!string.IsNullOrEmpty(consumerResult))
                                {
                                    reqId = Guid.NewGuid().ToString();

                                    _logger.LogInformation($"KafkaMerchantServiceTypeIntegrationService.DoWork consumer: {consumerResult} reqId: {reqId}");

                                    var dto = JsonSerializer.Deserialize<KafkaMerchantServiceTypeIntegrationRequest>(consumerResult);

                                    var merchantRepo = _unitOfWork.GetRepository<MerchantEntity>();
                                    var merchant = await merchantRepo.GetAll(false)
                                        .Where(x => x.Id == dto.MerchantId && !x.IsDeleted)
                                        .FirstOrDefaultAsync();

                                    if (merchant != null)
                                    {
                                        _logger.LogInformation($"updating reqId: {reqId}");

                                        // update
                                        merchant.MerchantServiceType = dto.MerchantServiceType;
                                        merchant.MdrRate = null;

                                        var subMerchants = await merchantRepo.GetAll(false).Where(x => x.MainBranchId == merchant.Id).ToListAsync();

                                        foreach (var subMerchant in subMerchants)
                                        {
                                            subMerchant.MerchantServiceType = dto.MerchantServiceType;
                                            subMerchant.MdrRate = null;
                                        }
                                        
                                        await merchantRepo.UpdateRangeAsync(merchant.Id, subMerchants.ToArray());
                                        await merchantRepo.UpdateAsync(merchant.Id, merchant);
                                        await merchantRepo.UnitOfWork.SaveChangesAsync();
                                    }

                                    _logger.LogInformation($"KafkaMerchantServiceTypeIntegrationService.DoWork save to db complete reqId: {reqId}");
                                }

                                consumerBuilder.Commit(consumer);
                            }
                        }
                        catch (Exception ex1)
                        {
                            _logger.LogError($"[ERROR] KafkaMerchantServiceTypeIntegrationService.DoWork message3: {ex1.Message} reqId: {reqId}");

                            consumerBuilder.Close();

                            await Task.Delay(TimeSpan.FromSeconds(10));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[ERROR] KafkaMerchantServiceTypeIntegrationService.DoWork message: {ex.Message}");
                }
            }
        }
    }
}
