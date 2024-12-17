using Argento.ReportingKafka.Application.Interface;
using Argento.ReportingKafka.Repository.Interface;
using Argento.ReportingKafka.Repository.Models;
using Argento.ReportingService.Utility;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Argento.ReportingKafka.Application.Services
{
    public class ConsumeTransactionService : IConsumeTransactionService
    {
        private readonly ILogger<ConsumeTransactionService> _logger;
        private string topic = "";
        private string groupId = "";
        private string bootstrapServers = "";
        private string autoOffsetReset = "";
        private readonly ITransactionRepository _transactionRepository;
        public ConsumeTransactionService(
            ILogger<ConsumeTransactionService> logger,
            IOptions<AppSettings> _appSettings,
            IConfiguration configuration,
            ITransactionRepository transactionRepository)
        {
            _logger = logger;
            topic = _appSettings.Value.KafkaSettings.NotifyTransactionTopic;
            groupId = $"{topic}_group1";
            bootstrapServers = _appSettings.Value.KafkaSettings.BootstrapServers;
            autoOffsetReset = _appSettings.Value.KafkaSettings.AutoOffsetReset;
            _transactionRepository = transactionRepository;
        }

        public async Task ConsumeMessages(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("ConsumeTransactionService.ConsumeMessages running");

                var config = new ConsumerConfig
                {
                    BootstrapServers = bootstrapServers,
                    GroupId = groupId,
                    AutoOffsetReset = autoOffsetReset == AutoOffsetReset.Earliest.ToString().ToLower() ? AutoOffsetReset.Earliest : AutoOffsetReset.Error
                };
                using (var consumerBuilder = new ConsumerBuilder<Ignore, string>(config).Build())
                {
                    consumerBuilder.Subscribe(topic);
                    var cancelToken = new CancellationTokenSource();
                    try
                    {
                        while (true)
                        {
                            var consumer = consumerBuilder.Consume(cancelToken.Token);
                            var consumeResult = consumer.Message.Value;
                            var requestId = consumer.Message.Key;

                            try
                            {

                                if (consumeResult != null)
                                {
                                    _logger.LogInformation($"[INFO] Received message: {consumeResult}");

                                    var dto = JsonSerializer.Deserialize<Transaction>(consumeResult);

                                    if (dto == null)
                                    {
                                        _logger.LogError($"[ERROR] ConsumeTransactionService.ConsumeMessages dto is cannot convert");

                                    }

                                    var transaction = await _transactionRepository.GetById(dto.Id);

                                    if (transaction == null)
                                    {
                                        _logger.LogInformation($"[INFO] ConsumeTransactionService create Id: {dto.Id} TransactionNo: {dto.TransactionNo}");
                                        await _transactionRepository.AddAsync(dto);
                                    }
                                    else if (dto.TransactionStatusId == 1 && transaction.TransactionStatusId > 1) 
                                    {
                                        _logger.LogInformation($"[INFO] ConsumeTransactionService, not overwrite transaction incomming status is `waiting to transfer`");
                                    }
                                    else
                                    {
                                        #region | set object |
                                        transaction.Amount = dto.Amount;
                                        transaction.ApproveStatusId = dto.ApproveStatusId;
                                        transaction.Balance = dto.Balance;
                                        transaction.BankCode = dto.BankCode;
                                        transaction.CancelAt = dto.CancelAt;
                                        transaction.CardMasking = dto.CardMasking;
                                        transaction.ChargeId = dto.ChargeId;
                                        transaction.CreatedByUserId = dto.CreatedByUserId;
                                        transaction.CreatedTimestamp = dto.CreatedTimestamp;
                                        transaction.DeletedByUserId = dto.DeletedByUserId;
                                        transaction.DeletedTimestamp = dto.DeletedTimestamp;
                                        transaction.Description = dto.Description;
                                        transaction.DeviceProfileId = dto.DeviceProfileId;
                                        transaction.Fee = dto.Fee;
                                        transaction.FeeVat = dto.FeeVat;
                                        transaction.FundingDetailId = dto.FundingDetailId;
                                        transaction.InternalOrder = dto.InternalOrder;
                                        transaction.InvoiceNo = dto.InvoiceNo;
                                        transaction.InvoiceRef = dto.InvoiceRef;
                                        transaction.IsDeleted = dto.IsDeleted;
                                        transaction.LastModifiedByUserId = dto.LastModifiedByUserId;
                                        transaction.LastModifiedTimestamp = dto.LastModifiedTimestamp;
                                        transaction.MainBranchId = dto.MainBranchId;
                                        transaction.Mdr = dto.Mdr;
                                        transaction.MerchantCode = dto.MerchantCode;
                                        transaction.MerchantId = dto.MerchantId;
                                        transaction.MerchantName = dto.MerchantName;
                                        transaction.MerchantServiceType = dto.MerchantServiceType;
                                        transaction.OrderId = dto.OrderId;
                                        transaction.Paid = dto.Paid;
                                        transaction.PaidAmount = dto.PaidAmount;
                                        transaction.PaidAt = dto.PaidAt;
                                        transaction.PaidCode = dto.PaidCode;
                                        transaction.PaidMessage = dto.PaidMessage;
                                        transaction.PaymentChannel = dto.PaymentChannel;
                                        transaction.ReconcileStatus = dto.ReconcileStatus;
                                        transaction.Reference1 = dto.Reference1;
                                        transaction.Reference2 = dto.Reference2;
                                        transaction.Reference3 = dto.Reference3;
                                        transaction.SourceName = dto.SourceName;
                                        transaction.TransactionNo = dto.TransactionNo;
                                        transaction.TransactionStatusId = dto.TransactionStatusId;
                                        transaction.TransferAmount = dto.TransferAmount;
                                        transaction.TransferDateTime = dto.TransferDateTime;
                                        transaction.TransferStatusId = dto.TransferStatusId;
                                        transaction.Vat = dto.Vat;
                                        transaction.WithHoldingTax = dto.WithHoldingTax;
                                        #endregion
                                        await _transactionRepository.UpdateAsync();
                                        _logger.LogInformation($"[INFO] ConsumeTransactionService save to db complete");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"[ERROR] ConsumeTransactionService.ConsumeMessages message2: {ex.Message}");
                                await Task.Delay(TimeSpan.FromSeconds(5));
                            }
                            finally
                            {
                                consumerBuilder.Commit(consumer);
                            }
                        }
                    }
                    catch (Exception ex1)
                    {
                        _logger.LogError($"[ERROR] ConsumeTransactionService.ConsumeMessages message3: {ex1.Message}");
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        consumerBuilder.Close();
                    }
                }
            }
        }
    }
}