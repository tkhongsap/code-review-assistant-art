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
    internal class KafkaCallbackUrlService : IScopedCallbackUrlService
    {
        private readonly ILogger _logger;
        private readonly AppSettings _appSettings;
        private readonly IUnitOfWorkReportingServiceDB _unitOfWork;

        private string topic = "";
        private string groupId = "";

        public KafkaCallbackUrlService(
            ILogger<KafkaCallbackUrlService> logger,
            IOptions<AppSettings> appSettings, 
            IUnitOfWorkReportingServiceDB unitOfWork)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
            _unitOfWork = unitOfWork;

            topic = _appSettings.MyConfig.UpdateCallbackUrlTopic;
            groupId = $"{topic}_group3";
        }

        public async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KafkaCallbackUrlService.DoWork init");

            var config = new ConsumerConfig
            {
                GroupId = groupId,
                BootstrapServers = _appSettings.MyConfig.KafkaUrl,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            //_logger.LogInformation($"KafkaCallbackUrlService.DoWork KafkaUrl: {_config.KafkaUrl}");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("KafkaCallbackUrlService.DoWork running");

                try
                {
                    using (var consumerBuilder = new ConsumerBuilder
                    <Ignore, string>(config).Build())
                    {
                        //_logger.LogInformation($"ScopedKafkaService.DoWork 1");

                        consumerBuilder.Subscribe(topic);

                        //_logger.LogInformation($"ScopedKafkaService.DoWork 2");

                        var cancelToken = new CancellationTokenSource();

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
                                    _logger.LogInformation($"KafkaCallbackUrlService.DoWork consumer: {consumerResult}");

                                    var dto = JsonSerializer.Deserialize<KafkaCallbackUrlRequest>(consumerResult);

                                    var merchantRepo = _unitOfWork.GetRepository<MerchantEntity>();
                                    var merchant = await merchantRepo.GetAll(false)
                                        .Where(x => x.Id == dto.MerchantId && !x.IsDeleted)
                                        .FirstOrDefaultAsync();

                                    if (merchant != null)
                                    {
                                        merchant.CallbackUrl = dto.CallbackUrl;
                                        

                                        await merchantRepo.UpdateAsync(merchant.Id, merchant);
                                        await merchantRepo.UnitOfWork.SaveChangesAsync();
                                    }

                                    _logger.LogInformation($"KafkaCallbackUrlService.DoWork save to db complete");
                                }

                                consumerBuilder.Commit(consumer);
                            }
                        }
                        catch (Exception ex1)
                        {
                            _logger.LogError($"[ERROR] KafkaCallbackUrlService.DoWork message3: {ex1.Message}");

                            consumerBuilder.Close();

                            await Task.Delay(TimeSpan.FromSeconds(10));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[ERROR] KafkaCallbackUrlService.DoWork message: {ex.Message}");
                }
            }
        }
    }
}
