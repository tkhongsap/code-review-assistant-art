using Arcadia.Extensions.DependencyInjection.Attributes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PG.PaymentService.BL.Interface;
using PG.PaymentService.DL.CallBack;
using PG.PaymentService.DL.Kbank;
using PG.PaymentService.DL.Scb;
using PG.PaymentService.Repository;
using PG.PaymentService.Repository.Model;
using PG.PaymentService.Utility;
using PG.PaymentService.Utility.Utils;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static PG.PaymentService.Utility.ArcadiaConstants;
using PG.PaymentService.Utility.Utils.Interface;

namespace PG.PaymentService.BL.Service
{
    [RegisterType(typeof(IWebHookService))]
    public partial class WebHookService : IWebHookService
    {
        private IUnitOfWorkPGPaymentServiceDB _unitOfWork;
        private readonly AppSettings _appSettings;
        private ITransactionService _transactionService;
        private readonly IKbankClient _httpClient;
        private readonly ILogger<WebHookService> _logger;
        private IMerchantService _merchantService;

        private readonly IJwtUtil _jwtUtil;
        public WebHookService(IOptions<AppSettings> appSettings
            , ITransactionService transactionService
            , IKbankClient httpClient
            , ILogger<WebHookService> logger
            , IUnitOfWorkPGPaymentServiceDB unitOfWork
            , IMerchantService merchantService
            , IServiceScopeFactory serviceScopeFactory
            , IMediator mediator
            , IJwtUtil jwtUtil
        )
        {
            _unitOfWork = unitOfWork;
            _appSettings = appSettings.Value;
            _transactionService = transactionService;
            _httpClient = httpClient;
            _logger = logger;
            _merchantService = merchantService;
            _jwtUtil = jwtUtil;
        }

        public async Task<string> RedirectCallBack(CallBackKbankRequest resource)
        {
            try
            {
                var transactionRepo = _unitOfWork.GetRepository<TransactionEntity>();
                var transaction = await transactionRepo.GetAll()
                                    .Where(x => x.ChargeId == resource.objectId && !x.IsDeleted)
                                    .Select(x => x)
                                    .FirstOrDefaultAsync();

                if (transaction != null)
                {
                    if (transaction.Paid)
                    {
                        //redirect to merchant success
                        string url = $"{_appSettings.URLstrings.KbankRedirectUrl}/{transaction.Id}/success";
                        return url;
                    }
                    else
                    {
                        //redirect to merchant fail
                        string url = $"{_appSettings.URLstrings.KbankRedirectUrl}/{transaction.Id}/failed";
                        return url;
                    }
                }
                else
                {
                    throw new Exception("No Transaction");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<UpdateNotifyQr> UpdateNotifyQr(NotifyKbankRequest resource)
        {
            try
            {
                var ret = new UpdateNotifyQr();

                var transactionRepo = _unitOfWork.GetRepository<TransactionEntity>();
                var transaction = await transactionRepo.GetAll()
                                    .Where(x => x.OrderId == resource.OrderId && !x.IsDeleted)
                                    .Select(x => x)
                                    .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    throw new Exception("No Transaction");
                }

                bool isUpdate = false;

                var paymentTerm = await _merchantService.GetPaymentTerm(transaction.MerchantId);
                var IsCompany = await _merchantService.IsCompany(transaction.MerchantId);

                if (transaction.TransactionStatusId != TransactionStatus.Paid)
                {
                    if (resource.status.ToLower() == "success" && resource.TransactionState == Transaction_state.Authorized)
                    {
                        if (resource.Amount < transaction.Amount)
                        {
                            var resultUpdatePaidNotFully = await _transactionService.PaidNotFully(transaction, resource.Id,
                                CustomStringDatetime.ConvertStringToDateTimeUTC(resource.Created, "yyyyMMddHHmmssfff"), null, null, IsCompany, resource.Amount);
                        }
                        else
                        {
                            var resultUpdateSuccess = await _transactionService.PaidSuccess(transaction, resource.Id,
                            CustomStringDatetime.ConvertStringToDateTimeUTC(resource.Created, "yyyyMMddHHmmssfff"), null, paymentTerm, IsCompany, resource.Amount);
                        }

                        isUpdate = true;
                    }
                    else if (transaction.TransactionStatusId == TransactionStatus.WaitForTransfer
                            && resource.status.ToLower() == "success" && resource.TransactionState == Transaction_state.Cancel)
                    {
                        var resultUpdateCancel = await _transactionService.PaidCancel(transaction);

                        isUpdate = true;
                    }
                    else if (transaction.TransactionStatusId == TransactionStatus.WaitForTransfer)
                    {
                        var resultUpdateFail = await _transactionService.PaidFail(transaction, resource.Id, DateTime.UtcNow, null, null);
                    }
                }
                else if (transaction.TransactionStatusId == TransactionStatus.Paid)
                {
                    if (resource.status.ToLower() == "success" &&
                    (resource.TransactionState == Transaction_state.VOID || resource.TransactionState == Transaction_state.Voided))
                    {
                        var resultUpdateVoid = await _transactionService.PaidVoid(transaction);

                        isUpdate = true;
                    }
                }

                if (isUpdate)
                {
                    var xTransaction = await transactionRepo.GetAll()
                                        .Where(x => x.TransactionNo == transaction.TransactionNo && !x.IsDeleted)
                                        .Select(x => x)
                                        .FirstOrDefaultAsync();

                    ret.IsSendNotify = true;
                    ret.MerchantId = transaction.MerchantId;
                    ret.Data = new CallBackUrlDto
                    {
                        TransactionNo = transaction.TransactionNo,
                        InvoiceNo = transaction.InvoiceNo,
                        Amount = Math.Round(transaction.Amount, 2),
                        PaidAmount = Math.Round(xTransaction.PaidAmount ?? 0, 2),
                        paymentChannel = transaction.PaymentChannel,
                        Status = transaction.TransactionStatusId.ToString(),
                        TransactionDate = CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(transaction.CreatedTimestamp, "yyyy-MM-dd HH:mm:ss")
                    };
                }
                else
                {
                    ret.MerchantId = transaction.MerchantId;
                    ret.Data = new CallBackUrlDto
                    {
                        TransactionNo = transaction.TransactionNo,
                    };
                }

                return ret;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<UpdateNotifyQr> UpdateNotifyCreditCard(NotifyCCKbankRequest resource)
        {
            try
            {
                var ret = new UpdateNotifyQr();

                var transactionRepo = _unitOfWork.GetRepository<TransactionEntity>();
                var transaction = await transactionRepo.GetAll()
                                    .Where(x => x.ChargeId == resource.Id &&
                                        x.TransactionStatusId == TransactionStatus.WaitForTransfer &&
                                        !x.IsDeleted)
                                    .Select(x => x).FirstOrDefaultAsync();

                if (transaction == null)
                {
                    throw new Exception("No Transaction");
                }

                bool isUpdate = false;

                var paymentTerm = await _merchantService.GetPaymentTerm(transaction.MerchantId);
                var IsCompany = await _merchantService.IsCompany(transaction.MerchantId);

                if (resource.Status.ToLower() == "success" && resource.TransactionState.ToLower() == "authorized")
                {
                    if (resource.Amount < transaction.Amount)
                    {
                        var resultUpdatePaidNotFully = await _transactionService.PaidNotFully(transaction, resource.Id,
                            CustomStringDatetime.ConvertStringToDateTimeUTC(resource.Created, "yyyyMMddHHmmssfff"), null, null, IsCompany, resource.Amount);
                    }
                    else
                    {
                        var resultUpdateSuccess = await _transactionService.PaidSuccess(transaction, resource.Id,
                            CustomStringDatetime.ConvertStringToDateTimeUTC(resource.Created, "yyyyMMddHHmmssfff"), resource.Sources.CardMasking, paymentTerm, IsCompany, resource.Amount);
                    }

                    isUpdate = true;
                }
                else if (resource.Status.ToLower() == "success" && resource.TransactionState.ToLower() == "cancel")
                {
                    await _transactionService.PaidCancel(transaction);
                    isUpdate = true;
                }
                else if (resource.Status.ToLower() == "success" && resource.TransactionState == "Pre-Authorized")
                {

                }
                else
                {
                    var resultUpdateFail = await _transactionService.PaidFail(transaction, resource.Id,
                        CustomStringDatetime.ConvertStringToDateTimeUTC(resource.Created, "yyyyMMddHHmmssfff"), null, null);

                    isUpdate = true;
                }

                if (isUpdate)
                {
                    var xTransaction = await transactionRepo.GetAll()
                                        .Where(x => x.TransactionNo == transaction.TransactionNo && !x.IsDeleted)
                                        .Select(x => x)
                                        .FirstOrDefaultAsync();

                    ret.IsSendNotify = true;
                    ret.MerchantId = transaction.MerchantId;
                    ret.Data = new CallBackUrlDto
                    {
                        TransactionNo = transaction.TransactionNo,
                        InvoiceNo = transaction.InvoiceNo,
                        Amount = Math.Round(transaction.Amount, 2),
                        PaidAmount = Math.Round(xTransaction.PaidAmount ?? 0, 2),
                        paymentChannel = transaction.PaymentChannel,
                        Status = transaction.TransactionStatusId.ToString(),
                        TransactionDate = CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(transaction.CreatedTimestamp, "yyyy-MM-dd HH:mm:ss")
                    };
                }
                else
                {
                    ret.MerchantId = transaction.MerchantId;
                    ret.Data = new CallBackUrlDto
                    {
                        TransactionNo = transaction.TransactionNo,
                    };
                }

                return ret;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> RedirectTo3rdParty(Guid merchantId, CallBackUrlDto resource, Guid ReqId)
        {
            //get merchant callback uri
            var merchantRepo = _unitOfWork.GetRepository<MerchantEntity>();
            var merchant = await merchantRepo.GetAll()
                                .Where(x => x.Id == merchantId && !x.IsDeleted)
                                .Select(x => new { x.CallbackUrl })
                                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(merchant.CallbackUrl))
            {
                _logger.LogInformation($"WebHookService.RedirectTo3rdParty no CallbackUrl reqId: {ReqId}");

                return "";
            }

            var callbackUrl = merchant.CallbackUrl;

            _logger.LogInformation($"WebHookService.RedirectTo3rdParty callbackUrl: {callbackUrl} reqId: {ReqId}");
            _logger.LogDebug($"WebHookService.RedirectTo3rdParty resource: {JsonSerializer.Serialize(resource)} reqId: {ReqId}");

            var bypassvalue = JsonSerializer.Serialize(resource, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            _logger.LogInformation($"WebHookService.RedirectTo3rdParty data: {bypassvalue} reqId: {ReqId}");

            var response = await _httpClient.PostFullUrl<string>(callbackUrl, bypassvalue);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogError($"WebHookService.RedirectTo3rdParty error: {errorMessage} reqId: {ReqId}");

                throw new Exception(errorMessage);
            }

            return callbackUrl;
        }

        public async Task<UpdateNotifyQrSCB> UpdateNotifyQrSCB(NotifyScbRequest resource, Guid reqId)
        {
            try
            {
                var ret = new UpdateNotifyQrSCB();

                var transactionRepo = _unitOfWork.GetRepository<TransactionEntity>();
                var transaction = await transactionRepo.GetAll()
                                        .Where(x => x.TransactionNo == resource.billPaymentRef1 && !x.IsDeleted)
                                        .Select(x => x)
                                        .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    throw new Exception("No Transaction");
                }

                bool isUpdate = false;

                var paymentTerm = await _merchantService.GetPaymentTerm(transaction.MerchantId);
                var IsCompany = await _merchantService.IsCompany(transaction.MerchantId);

                var dtFormat = resource.transactionDateandTime.Length == 29 ?
                                    "yyyy-MM-ddTHH:mm:ss.fffzzz" :
                               resource.transactionDateandTime.Length == 14 ?
                                    "yyyyMMddHHmmss" : "yyyy-MM-ddTHH:mm:sszzz";
                var dtConvert = CustomStringDatetime.ConvertStringToDateTimeUTC(resource.transactionDateandTime, dtFormat);

                var paidAmount = string.IsNullOrWhiteSpace(resource.amount) ? 0 : Math.Round(decimal.Parse(resource.amount), 2);

                if (transaction.TransactionStatusId == TransactionStatus.WaitForTransfer)
                {
                    if (resource.TransactionState == Transaction_state.Authorized)
                    {
                        if (paidAmount < transaction.Amount)
                        {
                            var resultUpdatePaidNotFully = await _transactionService.PaidNotFully(transaction, null,
                                dtConvert, null, null, IsCompany, paidAmount);
                        }
                        else
                        {
                            var resultUpdateSuccess = await _transactionService.PaidSuccess(transaction, null,
                            dtConvert, null, paymentTerm, IsCompany, paidAmount);
                        }

                        isUpdate = true;
                    }
                    else if (resource.TransactionState == Transaction_state.Cancel)
                    {
                        var resultUpdateCancel = await _transactionService.PaidCancel(transaction);

                        isUpdate = true;
                    }
                    else { }
                }
                else if (transaction.TransactionStatusId == TransactionStatus.Cancel)
                {
                    if (resource.TransactionState == Transaction_state.Authorized)
                    {
                        if (paidAmount < transaction.Amount)
                        {
                            var resultUpdatePaidNotFully = await _transactionService.PaidNotFully(transaction, null,
                                dtConvert, null, null, IsCompany, paidAmount);
                        }
                        else
                        {
                            var resultUpdateSuccess = await _transactionService.PaidSuccess(transaction, null,
                            dtConvert, null, paymentTerm, IsCompany, paidAmount);
                        }

                        isUpdate = true;
                    }
                    else { }
                }
                else if (transaction.TransactionStatusId == TransactionStatus.Paid)
                {
                    if (resource.TransactionState == Transaction_state.Authorized)
                    {
                        _logger.LogInformation($"WebHookService.UpdateNotifyQrSCB transaction paid already reqId: {reqId}");
                    }
                }
                else { }

                if (isUpdate)
                {
                    var xTransaction = await transactionRepo.GetAll()
                                        .Where(x => x.TransactionNo == transaction.TransactionNo && !x.IsDeleted)
                                        .Select(x => x)
                                        .FirstOrDefaultAsync();

                    ret.IsSendNotify = true;
                    ret.MerchantId = transaction.MerchantId;
                    ret.Data = new CallBackUrlDto
                    {
                        TransactionNo = transaction.TransactionNo,
                        InvoiceNo = transaction.InvoiceNo,
                        Amount = Math.Round(transaction.Amount, 2),
                        PaidAmount = Math.Round(xTransaction.PaidAmount ?? 0, 2),
                        paymentChannel = transaction.PaymentChannel,
                        Status = transaction.TransactionStatusId.ToString(),
                        TransactionDate = CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(transaction.CreatedTimestamp, "yyyy-MM-dd HH:mm:ss")
                    };
                }
                else
                {
                    ret.MerchantId = transaction.MerchantId;
                    ret.Data = new CallBackUrlDto
                    {
                        TransactionNo = transaction.TransactionNo,
                    };
                }

                return ret;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR WebHookService.UpdateNotifyQrSCB message: {ex.Message}");

                throw new Exception(ex.Message);
            }
        }

        public async Task<UpdateNotifyQrSCB> UpdateNotifyDeepLinkSCB(NotifyScbDeepLinkRequest resource, Guid reqId)
        {
            try
            {
                var ret = new UpdateNotifyQrSCB();

                var transactionRepo = _unitOfWork.GetRepository<TransactionEntity>();
                var transaction = await transactionRepo.GetAll()
                                        .Where(x => x.TransactionNo == resource.TransactionNo && !x.IsDeleted)
                                        .Select(x => x)
                                        .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    throw new Exception("No Transaction");
                }

                bool isUpdate = false;

                var paymentTerm = await _merchantService.GetPaymentTerm(transaction.MerchantId);
                var IsCompany = await _merchantService.IsCompany(transaction.MerchantId);

                var dtFormat = resource.PaidAt.Length == 29 ?
                                    "yyyy-MM-ddTHH:mm:ss.fffzzz" :
                               resource.PaidAt.Length == 14 ?
                                    "yyyyMMddHHmmss" : "yyyy-MM-ddTHH:mm:sszzz";
                var dtConvert = CustomStringDatetime.ConvertStringToDateTimeUTC(resource.PaidAt, dtFormat);

                var paidAmount = string.IsNullOrWhiteSpace(resource.PaidAmount.ToString()) ? 0 : Math.Round(resource.PaidAmount, 2);

                if (transaction.TransactionStatusId == TransactionStatus.WaitForTransfer)
                {
                    if (resource.TransactionState == Transaction_state.Authorized)
                    {
                        if (paidAmount < transaction.Amount)
                        {
                            var resultUpdatePaidNotFully = await _transactionService.PaidNotFully(transaction, null,
                                dtConvert, null, null, IsCompany, paidAmount);
                        }
                        else
                        {
                            var resultUpdateSuccess = await _transactionService.PaidSuccess(transaction, null,
                            dtConvert, null, paymentTerm, IsCompany, paidAmount);
                        }

                        isUpdate = true;
                    }
                    else if (resource.TransactionState == Transaction_state.Cancel)
                    {
                        var resultUpdateCancel = await _transactionService.PaidCancel(transaction);

                        isUpdate = true;
                    }
                    else { }
                }
                else if (transaction.TransactionStatusId == TransactionStatus.Cancel)
                {
                    if (resource.TransactionState == Transaction_state.Authorized)
                    {
                        if (paidAmount < transaction.Amount)
                        {
                            var resultUpdatePaidNotFully = await _transactionService.PaidNotFully(transaction, null,
                                dtConvert, null, null, IsCompany, paidAmount);
                        }
                        else
                        {
                            var resultUpdateSuccess = await _transactionService.PaidSuccess(transaction, null,
                            dtConvert, null, paymentTerm, IsCompany, paidAmount);
                        }

                        isUpdate = true;
                    }
                    else { }
                }
                else if (transaction.TransactionStatusId == TransactionStatus.Paid)
                {
                    if (resource.TransactionState == Transaction_state.Authorized)
                    {
                        _logger.LogInformation($"WebHookService.UpdateNotifyDeepLinkSCB transaction paid already reqId: {reqId}");
                    }
                }
                else { }

                if (isUpdate)
                {
                    var xTransaction = await transactionRepo.GetAll()
                                        .Where(x => x.TransactionNo == transaction.TransactionNo && !x.IsDeleted)
                                        .Select(x => x)
                                        .FirstOrDefaultAsync();

                    ret.IsSendNotify = true;
                    ret.MerchantId = transaction.MerchantId;
                    ret.Data = new CallBackUrlDto
                    {
                        TransactionNo = transaction.TransactionNo,
                        InvoiceNo = transaction.InvoiceNo,
                        Amount = Math.Round(transaction.Amount, 2),
                        PaidAmount = Math.Round(xTransaction.PaidAmount ?? 0, 2),
                        paymentChannel = transaction.PaymentChannel,
                        Status = transaction.TransactionStatusId.ToString(),
                        TransactionDate = CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(transaction.CreatedTimestamp, "yyyy-MM-dd HH:mm:ss")
                    };
                }
                else
                {
                    ret.MerchantId = transaction.MerchantId;
                    ret.Data = new CallBackUrlDto
                    {
                        TransactionNo = transaction.TransactionNo,
                    };
                }

                return ret;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR WebHookService.UpdateNotifyDeepLinkSCB message: {ex.Message}");

                throw new Exception(ex.Message);
            }
        }

        public async Task<UpdateNotifyQrSCB> UpdateNotifyCreditCardSCB(NotifyScbCreditCardRequest resource, Guid reqId)
        {
            try
            {
                var ret = new UpdateNotifyQrSCB();

                var transactionRepo = _unitOfWork.GetRepository<TransactionEntity>();
                var transaction = await transactionRepo.GetAll()
                                        .Where(x => x.TransactionNo == resource.invoiceNo && !x.IsDeleted)
                                        .Select(x => x)
                                        .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    throw new Exception("No Transaction");
                }

                bool isUpdate = false;

                var paymentTerm = await _merchantService.GetPaymentTerm(transaction.MerchantId);
                var IsCompany = await _merchantService.IsCompany(transaction.MerchantId);

                var dtFormat = resource.transactionDateTime.Length == 29 ?
                                    "yyyy-MM-ddTHH:mm:ss.fffzzz" :
                               resource.transactionDateTime.Length == 14 ?
                                    "yyyyMMddHHmmss" : "yyyy-MM-ddTHH:mm:sszzz";
                var dtConvert = CustomStringDatetime.ConvertStringToDateTimeUTC(resource.transactionDateTime, dtFormat);
                var paidAmount = string.IsNullOrWhiteSpace(resource.amount) ? 0 : Math.Round(decimal.Parse(resource.amount), 2);

                if (transaction.TransactionStatusId == TransactionStatus.WaitForTransfer)
                {
                    if (resource.TransactionState == Transaction_state.Authorized)
                    {
                        if (paidAmount < transaction.Amount)
                        {
                            var resultUpdatePaidNotFully = await _transactionService.PaidNotFully(transaction, null,
                                dtConvert, null, null, IsCompany, paidAmount);
                        }
                        else
                        {
                            var resultUpdateSuccess = await _transactionService.PaidSuccess(transaction, null,
                            dtConvert, resource.cardNo, paymentTerm, IsCompany, paidAmount);

                        }

                        isUpdate = true;
                    }
                    else if (resource.TransactionState == Transaction_state.Cancel)
                    {
                        var resultUpdateCancel = await _transactionService.PaidCancel(transaction);

                        isUpdate = true;
                    }
                    else if (resource.TransactionState == Transaction_state.Failed)
                    {
                        var resultUpdateFail = await _transactionService.PaidFail(transaction, null, dtConvert, null, null);

                        isUpdate = true;
                    }
                    else { }
                }
                else if (transaction.TransactionStatusId == TransactionStatus.Cancel)
                {
                    if (resource.TransactionState == Transaction_state.Authorized)
                    {
                        if (paidAmount < transaction.Amount)
                        {
                            var resultUpdatePaidNotFully = await _transactionService.PaidNotFully(transaction, null,
                                dtConvert, null, null, IsCompany, paidAmount);
                        }
                        else
                        {
                            var resultUpdateSuccess = await _transactionService.PaidSuccess(transaction, null,
                            dtConvert, resource.cardNo, paymentTerm, IsCompany, paidAmount);
                        }

                        isUpdate = true;
                    }
                    else { }
                }
                else if (transaction.TransactionStatusId == TransactionStatus.Paid)
                {
                    if (resource.TransactionState == Transaction_state.Authorized)
                    {
                        _logger.LogInformation($"WebHookService.UpdateNotifyCreditCardSCB transaction paid already reqId: {reqId}");
                    }
                }
                else { }

                if (isUpdate)
                {
                    var xTransaction = await transactionRepo.GetAll()
                                        .Where(x => x.TransactionNo == transaction.TransactionNo && !x.IsDeleted)
                                        .Select(x => x)
                                        .FirstOrDefaultAsync();

                    ret.IsSendNotify = true;
                    ret.MerchantId = transaction.MerchantId;
                    ret.Data = new CallBackUrlDto
                    {
                        TransactionNo = transaction.TransactionNo,
                        InvoiceNo = transaction.InvoiceNo,
                        Amount = Math.Round(transaction.Amount, 2),
                        PaidAmount = Math.Round(xTransaction.PaidAmount ?? 0, 2),
                        paymentChannel = transaction.PaymentChannel,
                        Status = transaction.TransactionStatusId.ToString(),
                        TransactionDate = CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(transaction.CreatedTimestamp, "yyyy-MM-dd HH:mm:ss")
                    };
                }
                else
                {
                    ret.MerchantId = transaction.MerchantId;
                    ret.Data = new CallBackUrlDto
                    {
                        TransactionNo = transaction.TransactionNo,
                    };
                }

                return ret;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
