using Arcadia.Extensions.DependencyInjection.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PG.PaymentService.BL.CustomHttpExceptions;
using PG.PaymentService.BL.Interface;
using PG.PaymentService.DL;
using PG.PaymentService.DL.Kbank;
using PG.PaymentService.DL.Merchants;
using PG.PaymentService.DL.Payments;
using PG.PaymentService.DL.Scb;
using PG.PaymentService.DL.Transactions;
using PG.PaymentService.DL.ArtChain;
using PG.PaymentService.Repository;
using PG.PaymentService.Repository.Model;
using PG.PaymentService.Utility;
using PG.PaymentService.Utility.Utils;
using PG.PaymentService.Utility.Utils.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static PG.PaymentService.Utility.ArcadiaConstants;
using PG.PaymentService.DL.Bay;

namespace PG.PaymentService.BL.Service
{
    [RegisterType(typeof(IPaymentsService))]
    public partial class PaymentsService : IPaymentsService
    {
        private readonly IUnitOfWorkPGPaymentServiceDB _unitOfWork;
        private readonly AppSettings _appSettings;
        private readonly ITransactionService _transactionService;
        private readonly ILogger<PaymentsService> _logger;
        private readonly IJwtUtil _jwtUtil;
        private readonly IMerchantService _merchantService;
        private readonly IQrCoderService _qrCoderService;
        private readonly PaymentsServiceInternal _paymentServiceInternal;
        private readonly IQrCodeService _qrCodeService;
        private readonly IKbankService _kBankService;
        private readonly IScbService _scbService;
        private readonly IBayService _bayService;
        private readonly IArtChainService _artChainService;
        private readonly IHelperService _helperService;
        private readonly IKTBService _ktbService;

        const int maxTimeOut = 10;

        public PaymentsService(
            IOptions<AppSettings> appSettings
            , IKbankService kbankService
            , IScbService scbService
            , IBayService bayService
            , ILinepayService linepayService
            , ITruemoneyService truemoneyService
            , ITransactionService transactionService
            , IUnitOfWorkPGPaymentServiceDB unitOfWork
            , ILogger<PaymentsService> logger
            , IJwtUtil jwtUtil
            , IMerchantService merchantService
            , IQrCoderService qrCoderService
            , PaymentsServiceInternal paymentServiceInternal
            , IQrCodeService qrCodeService
            , IWebHookService webHookService
            , IArtChainService artChainService
            , IHelperService helperService
            , IKTBService ktbService
        )
        {
            _unitOfWork = unitOfWork;
            _appSettings = appSettings.Value;
            _transactionService = transactionService;
            _kBankService = kbankService;
            _scbService = scbService;
            _bayService = bayService;
            _logger = logger;
            _jwtUtil = jwtUtil;
            _merchantService = merchantService;
            _qrCoderService = qrCoderService;
            _paymentServiceInternal = paymentServiceInternal;
            _qrCodeService = qrCodeService;
            _artChainService = artChainService;
            _helperService = helperService;
            _ktbService = ktbService;
        }

        public async Task<SourceReponseDto> CreateSource(SourceRequestDto request, Guid merchantId)
        {
            string reqId = request.MerchantID + "|" + request.InvoiceNo + "|" + request.Amount.ToString();
            this._logger.LogInformation($"PaymentService.CreateSource : ReqId : {reqId}");

            if (merchantId != Guid.Parse(request.MerchantID))
            {
                throw new MerchantIdNotFoundException();
            }

            var merchantRepo = _unitOfWork.GetRepository<MerchantEntity>();
            var merchant = await merchantRepo.GetAll(false).Where(x => x.Id == merchantId && !x.IsDeleted)
                                    .Select(x => new
                                    {
                                        x.Id,
                                        x.SecretKey,
                                    })
                                    .FirstOrDefaultAsync();

            if (merchant is null)
            {
                throw new MerchantIdNotFoundException();
            }

            // validate amount
            var amountDto = await _qrCodeService.GetAmount();

            if (request.Amount < amountDto.Minimum || request.Amount > amountDto.Maximum)
            {
                throw new AmountOutOfRangeException();
            }

            // jwt
            string secretKey = merchant.SecretKey;
            string transacionId = Guid.NewGuid().ToString();
            var token = await _jwtUtil.EncodeToken(request, secretKey, transacionId);
            this._logger.LogInformation($"PaymentService.CreateSource - ReqId : {reqId}, Source ID : {token}");

            return new SourceReponseDto
            {
                respCode = "0000",
                respDesc = "success",
                SourceId = token,
                webPaymentUrl = $"{_appSettings.webPaymentUrl}/{token}",
                transactionId = transacionId
            };
        }


        public async Task<ChargeQRReponseDto> CreateChargeQr(ChargeQrRequestDto request, Guid merchantId_tmp, string sourceName, Guid reqId)
        {
            _logger.LogInformation($"PaymentService.CreateChargeQr xxx request: {JsonSerializer.Serialize(request)}");

            try
            {
                // get jwt invalid
                var sourceId = request.SourceId;
                _logger.LogDebug($"PaymentService.CreateChargeQr sourceId: {sourceId}");

                SourceRequestDto jwtInvalid = null;
                Guid jwtInvalidMerchantId = Guid.Empty;

                try
                {
                    jwtInvalid = await _jwtUtil.DecodeTokenWithoutValidate(sourceId);
                    jwtInvalidMerchantId = Guid.Parse(jwtInvalid.MerchantID);
                }
                catch (Exception)
                {
                    throw new TokenDecodeException();
                }

                var merchant = await _helperService.GetMerchantPaymentChannelMdrRate(jwtInvalidMerchantId, PaymentChannelType.PromptPay.Name);

                // get jwt valid
                // TODO: DUPLICATE Code, Considerate to refactor later
                var jwtValid = await _jwtUtil.DecodeToken(sourceId, merchant.SecretKey);
                await CheckDuplicateTransction(jwtValid.TransactionId);
                Guid jwtValidMerchantId = Guid.Parse(jwtValid.MerchantID);

                var payment = await CreatePayment(jwtValid.TransactionId, merchant.PaymentChannel, merchant.MdrRate, sourceName, request.Description, merchant.MerchantCode, merchant.MerchantName, merchant.IsCompany, jwtValid, merchant.MainBranchId, null);

                var transactionRepo = _unitOfWork.GetRepository<TransactionEntity>();
                var transactionData = await _transactionService.GetTransactionById(payment.Id);
                var transactionNo = transactionData.TransactionNo;

                var qrTimeOut = await _paymentServiceInternal.GetQrTimeOut(request.qrTimeOut, 1, 10);

                var updateTransaction = transactionData;

                try
                {
                    string qrImage = "";

                    if (payment.bankCode == BankType.KBank.Name)
                    {
                        var result = new PromptPayKbankDto
                        {
                            merchantId = jwtValidMerchantId,
                            transactionNo = transactionNo,
                            Amount = jwtValid.Amount,
                            Currency = jwtValid.Currency,
                            expiry = qrTimeOut,
                            secretKey = payment.secretApiKey,
                            Description = jwtValid.InvoiceNo,
                            sof = "ThaiQR"
                        };

                        var bypassvalue = JsonSerializer.Serialize(result, new JsonSerializerOptions()
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        var responseKBank = await _kBankService.CreateQr("qr", bypassvalue);

                        // update order id to transaction 
                        updateTransaction.ChargeId = responseKBank.id;
                        updateTransaction.OrderId = responseKBank.order_id;
                        qrImage = responseKBank.qrImage;
                    }
                    else if (payment.bankCode == BankType.SCB.Name)
                    {
                        var result = new PromptPaySCBDto
                        {
                            merchantId = jwtValidMerchantId,
                            transactionNo = transactionNo,
                            Amount = jwtValid.Amount,
                            Ref1 = transactionNo,
                            Ref2 = RandomString.Generate(12),
                            Ref3 = payment.Abbv,
                            expiry = qrTimeOut,
                            apiKey = payment.apiKey,
                            secretKey = payment.secretApiKey,
                            billerid = payment.billerId,

                        };

                        var bypassvalue = JsonSerializer.Serialize(result, new JsonSerializerOptions()
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        var responseSCB = await _scbService.SendSCBQrAsync("qr30", bypassvalue, reqId);
                        qrImage = responseSCB.qrImage;
                    }
                    else if (payment.bankCode == BankType.BAY.Name)
                    {
                        // BAY : Default timeout = 3 minutes
                        qrTimeOut = 3;

                        var result = new PromptPayBAYDto
                        {
                            merchantId = payment.bankMerchantId,
                            transactionNo = transactionNo,
                            amount = jwtValid.Amount,
                            ref1 = transactionNo,
                            ref2 = RandomString.Generate(12),
                            description = "N/A",
                            expiry = qrTimeOut,
                            apiKey = payment.apiKey,
                            rSAPublicKey = payment.publicApiKey,
                            billerId = payment.billerId,
                            paymentChannelType = BayChannelCode.PromptPay
                        };

                        var bypassvalue = JsonSerializer.Serialize(result, new JsonSerializerOptions()
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        var responseBAY = await _bayService.SendBAYQrAsync("qr30", bypassvalue);

                        updateTransaction.ChargeId = responseBAY.trxId;
                        qrImage = _qrCoderService.CreateFromUrl(responseBAY.qrImage);
                        qrTimeOut = responseBAY.qrTimeout;
                    }
                    else
                    {
                        throw new Exception("Not Support Now!");
                    }

                    if (jwtValid.Description == "LinkToPay")
                    {
                        updateTransaction.InvoiceNo = transactionNo;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(updateTransaction.InvoiceNo))
                        {
                            updateTransaction.InvoiceNo = transactionNo;
                        }
                    }

                    var resultUpdate = await _transactionService.UpdateTransaction(updateTransaction);

                    return new ChargeQRReponseDto
                    {
                        respCode = "0000",
                        respDesc = "success",
                        qrImage = qrImage,
                        transactionNo = transactionNo,
                        qrId = updateTransaction.ChargeId,
                        qrTimeOut = qrTimeOut,
                    };
                }
                catch (Exception)
                {
                    // update transaction fail
                    await _paymentServiceInternal.updateTransactionFailAndSendNotify(updateTransaction, reqId);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR PaymentService.CreateChargeQr message: {ex.Message}");

                if (ex is ICustomHttpException)
                {
                    throw;
                }

                //throw new InvalidOperationException("Fail to create charge qr: " + ex.Message);
                throw new Exception($"Fail to create charge qr: {ex.Message}");
            }
        }

        public async Task<ChargeReponseDto> CreateChargeCreditcard(ChargeCreditcardRequestDto request, Guid merchantId_tmp, string sourceName, Guid reqId)
        {
            try
            {
                // get jwt invalid
                var sourceId = request.SourceId;
                _logger.LogInformation($"PaymentService.CreateChargeCreditcard sourceId: {sourceId}");

                var returnResult = new ChargeReponseDto();

                SourceRequestDto jwtInvalid = null;
                Guid jwtInvalidMerchantId = Guid.Empty;

                try
                {
                    jwtInvalid = await _jwtUtil.DecodeTokenWithoutValidate(sourceId);
                    jwtInvalidMerchantId = Guid.Parse(jwtInvalid.MerchantID);
                }
                catch (Exception)
                {
                    throw new TokenDecodeException();
                }

                var merchant = await _helperService.GetMerchantPaymentChannelMdrRate(jwtInvalidMerchantId, PaymentChannelType.CreditCard.Name);

                // get jwt valid
                // TODO: DUPLICATE Code, Considerate to refactor later
                var jwtValid = await _jwtUtil.DecodeToken(sourceId, merchant.SecretKey);
                await CheckDuplicateTransction(jwtValid.TransactionId);
                Guid jwtValidMerchantId = Guid.Parse(jwtValid.MerchantID);

                var payment = await CreatePayment(jwtValid.TransactionId, merchant.PaymentChannel, merchant.MdrRate, sourceName, request.Description, merchant.MerchantCode, merchant.MerchantName, merchant.IsCompany, jwtValid, merchant.MainBranchId, null);

                var transactionRepo = _unitOfWork.GetRepository<TransactionEntity>();
                var transactionList = await _transactionService.GetTransactionById(payment.Id);
                var transactionNo = transactionList.TransactionNo;

                if (payment.bankCode == BankType.KBank.Name)
                {
                    var updateTransaction = transactionList;

                    try
                    {
                        // apiType 1 = Direct API, apiType 2 = Embedded UI
                        if (request.apiType == 2)
                        {
                            var result = new CreditCardPaymentLinkDto
                            {
                                ActiveTime = DateTime.Now.ToString("yyyyMMddHHmmss"),
                                Amount = jwtValid.Amount,
                                Currency = jwtValid.Currency,
                                ExpireTime = DateTime.Now.AddMinutes(30).ToString("yyyyMMddHHmmss"),
                                ReferenceNumber = transactionNo,
                                SecretKey = merchant.SecretKey,
                                ServiceName = "Argento-" + DateTime.Now.ToString("yyyyMMdd")
                            };
                            var bypassvalue = JsonSerializer.Serialize(result, new JsonSerializerOptions()
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                            _logger.LogInformation($"PaymentsService.CreateChargeCreditcard bypassvalue: {bypassvalue}");

                            var sourceCreditCardKbank = new SourceCreditCardKbank
                            {
                                Amount = jwtValid.Amount,
                                APIKey = payment.publicApiKey,
                                BankMerchantId = payment.bankMerchantId,
                                InvoiceNo = jwtInvalid.InvoiceNo,
                                TransactionNo = transactionNo,
                                MerchantId = jwtInvalidMerchantId.ToString(),
                                MerchantName = merchant.MerchantName,
                                TimeoutInSecond = request.Expiry.HasValue ? request.Expiry.Value * 60 : 600 // 10 minutes from now
                            };
                            var jwtResult = await _jwtUtil.EncodeTokenCreditCardLandingPage(sourceCreditCardKbank, merchant.SecretKey);
                            var redirectUrl = _appSettings.webPaymentUrl + "/creditCardRedirect/" + jwtResult;

                            return returnResult = new ChargeReponseDto
                            {
                                respCode = "0000",
                                respDesc = "success",
                                redirect_url = redirectUrl,
                                transactionNo = transactionNo,
                            };
                        }
                        else
                        {
                            var result = new CreditCardDto
                            {
                                MerchantId = jwtValidMerchantId,
                                transactionNo = transactionNo,
                                amount = jwtValid.Amount,
                                currency = jwtValid.Currency,
                                cardName = request.CardName,
                                cardNumber = request.CardNo,
                                cardCvv = request.Cvv,
                                cardExpiryMonth = request.Month,
                                cardExpiryYear = request.Year,
                                ref1 = transactionNo,
                                ref2 = "REF02",
                                mid = payment.bankMerchantId,
                                secretKey = payment.secretApiKey,
                                publicKey = payment.publicApiKey,
                                Description = jwtValid.InvoiceNo,
                            };

                            var bypassvalue = JsonSerializer.Serialize(result, new JsonSerializerOptions()
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                            _logger.LogInformation($"PaymentsService.CreateChargeCreditcard bypassvalue: {bypassvalue}");

                            var responseKBank = await _kBankService.SendKBankCardPaymentAsync("creditcard", bypassvalue);

                            //check credit card payment
                            if (responseKBank.status == "success" && responseKBank.transaction_state == Transaction_state.Authorized)
                            {
                                var paymentTerm = await _merchantService.GetPaymentTerm(merchant.Id);
                                var IsCompany = await _merchantService.IsCompany(merchant.Id);

                                var resultUpdateSuccess = await _transactionService.PaidSuccess(updateTransaction, responseKBank.id, DateTime.UtcNow, null, paymentTerm, IsCompany, responseKBank.amount);
                            }
                            else if (responseKBank.status == "success" && responseKBank.transaction_state == Transaction_state.Pre_Authorized)
                            {
                                var resultUpdatePreAuhten = await _transactionService.PaidPreAuthen(updateTransaction, responseKBank.id);
                            }
                            else
                            {
                                var resultUpdateFail = await _transactionService.PaidFail(updateTransaction, responseKBank.id, DateTime.UtcNow, null, null);
                            }

                            if (jwtValid.Description == "LinkToPay")
                            {
                                updateTransaction.InvoiceNo = transactionNo;
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(updateTransaction.InvoiceNo))
                                {
                                    updateTransaction.InvoiceNo = transactionNo;
                                }
                            }
                            var resultUpdate = await _transactionService.UpdateTransaction(updateTransaction);

                            returnResult = new ChargeReponseDto
                            {
                                respCode = "0000",
                                respDesc = "success",
                                redirect_url = responseKBank.redirect_url,
                                transactionNo = responseKBank.transaction_no,
                            };
                        }
                    }
                    catch (Exception)
                    {
                        if (jwtValid != null && !string.IsNullOrEmpty(jwtValid.InvoiceNo))
                        {
                            updateTransaction.InvoiceNo = jwtValid.InvoiceNo;
                        }
                        else
                        {
                            updateTransaction.InvoiceNo = transactionNo;
                        }
                        // update transaction fail

                        await _paymentServiceInternal.updateTransactionFailAndSendNotify(updateTransaction, reqId);

                        throw;
                    }
                }
                else if (payment.bankCode == BankType.SCB.Name)
                {
                    var result = new CreditCardSCBDto
                    {
                        MerchantId = payment.bankMerchantId,
                        transactionNo = transactionNo,
                        amount = jwtValid.Amount,
                        secretKey = payment.PaymentChannelSecretApiKey,
                        Description = jwtValid.InvoiceNo,
                    };

                    var bypassvalue = JsonSerializer.Serialize(result, new JsonSerializerOptions()
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    _logger.LogInformation($"PaymentsService.CreateChargeCreditcard bypassvalue: {bypassvalue}");

                    var updateTransaction = transactionList;

                    try
                    {
                        var responseSCB = await _scbService.SendSCBCreditCardAsync("creditcard/create", bypassvalue);

                        if (jwtValid.Description == "LinkToPay")
                        {
                            updateTransaction.InvoiceNo = transactionNo;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(updateTransaction.InvoiceNo))
                            {
                                updateTransaction.InvoiceNo = transactionNo;
                            }
                        }
                        var resultUpdate = await _transactionService.UpdateTransaction(updateTransaction);

                        returnResult = new ChargeReponseDto
                        {
                            respCode = "0000",
                            respDesc = "success",
                            redirect_url = responseSCB.WebPaymentUrl,
                            transactionNo = transactionNo,
                        };
                    }
                    catch (Exception)
                    {
                        // update transaction fail
                        if (jwtValid != null && !string.IsNullOrEmpty(jwtValid.InvoiceNo))
                        {
                            updateTransaction.InvoiceNo = jwtValid.InvoiceNo;
                        }
                        else
                        {
                            updateTransaction.InvoiceNo = transactionNo;
                        }

                        await _paymentServiceInternal.updateTransactionFailAndSendNotify(updateTransaction, reqId);

                        throw;
                    }
                }

                return returnResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR PaymentService.CreateChargeCreditCard message: {ex.Message}");

                if (ex is ICustomHttpException)
                {
                    throw;
                }

                throw new Exception($"Fail to create charge creditcard: {ex.Message}");
            }
        }

        public async Task<ChargeAlipayWeChatPayBscanCReponseDto> CreateChargeAlipayBscanC(ChargeAlipayWeChatPayBscanCRequestDto request, Guid merchantId_tmp, string sourceName)
        {
            try
            {
                // get jwt invalid
                var sourceId = request.SourceId;
                _logger.LogInformation($"PaymentService.CreateChargeAlipayBscanC sourceId: {sourceId}");

                SourceRequestDto jwtInvalid = null;
                Guid jwtInvalidMerchantId = Guid.Empty;

                try
                {
                    jwtInvalid = await _jwtUtil.DecodeTokenWithoutValidate(sourceId);
                    jwtInvalidMerchantId = Guid.Parse(jwtInvalid.MerchantID);
                }
                catch (Exception)
                {
                    throw new TokenDecodeException();
                }

                var merchant = await _helperService.GetMerchantPaymentChannelMdrRate(jwtInvalidMerchantId, PaymentChannelType.AliPayBtoC.Name);

                // get jwt valid
                var jwtValid = await _jwtUtil.DecodeToken(sourceId, merchant.SecretKey);
                await CheckDuplicateTransction(jwtValid.TransactionId);
                Guid jwtValidMerchantId = Guid.Parse(jwtValid.MerchantID);

                var payment = await CreatePayment(jwtValid.TransactionId, merchant.PaymentChannel, merchant.MdrRate, sourceName, request.Description, merchant.MerchantCode, merchant.MerchantName, merchant.IsCompany, jwtValid, merchant.MainBranchId, request.DeviceProfileId);

                var transactionRepo = _unitOfWork.GetRepository<TransactionEntity>();
                var transactionList = await _transactionService.GetTransactionById(payment.Id);
                var transactionNo = transactionList.TransactionNo;
                var updateTransaction = transactionList;

                try
                {
                    var requestDto = new AlipayWeChatPayBscanCRequestToSCB
                    {
                        DeviceProfileId = request.DeviceProfileId,
                        TransactionNo = transactionNo,
                        MerchantId = jwtValidMerchantId.ToString(),
                        TranType = "A",
                        CompanyId = _appSettings.SCBCompanyId,
                        TerminalId = payment.terminalId,
                        OutTradeNo = _appSettings.isProduction ? transactionNo : _appSettings.outTradeNoPrefix + transactionNo,
                        buyerIdentityCode = request.buyerIdentityCode,
                        Amount = jwtValid.Amount.ToString("#,###.##"),
                        ApiKey = payment.apiKey,
                        SecretKey = payment.secretApiKey
                    };

                    var requestDtoStr = JsonSerializer.Serialize<AlipayWeChatPayBscanCRequestToSCB>(requestDto);

                    var responseAlipayWeChatPay = await _scbService.SendSCBAlipayWeChatPayBscanCAsync("alipaywechatpay/bscanc", requestDtoStr);

                    // update transaction payment and notify
                    var updateAndNotifyResponse = await _paymentServiceInternal.updatePaymentStatusAndSendNotifyOfAlipayBScanC(responseAlipayWeChatPay.TransactionId, responseAlipayWeChatPay.TradeState, merchant.Id, updateTransaction, jwtValid.Amount);

                    // transactionId from db is not equal transactionId from AlipayWeChatPay response
                    //updateTransaction.OrderId = responseAlipayWeChatPay.Transaction_no.ToString();

                    if (jwtValid.Description == "LinkToPay")
                    {
                        updateTransaction.InvoiceNo = transactionNo;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(updateTransaction.InvoiceNo))
                        {
                            updateTransaction.InvoiceNo = transactionNo;
                        }
                    }
                    var resultUpdate = await _transactionService.UpdateTransaction(updateTransaction);

                    return new ChargeAlipayWeChatPayBscanCReponseDto
                    {
                        respCode = "0000",
                        respDesc = "success",
                        transactionNo = transactionNo,
                        //codeUrl = responseAlipayWeChatPay.CodeUrl,
                        //prepayId = responseAlipayWeChatPay.PrepayId
                    };
                }
                catch (Exception ex2)
                {

                    await _paymentServiceInternal.updateTransactionFailAndSendNotify(updateTransaction, merchantId_tmp);
                    throw new Exception(ex2.Message);
                }
            }
            catch (Exception ex)
            {
                if (ex is ICustomHttpException)
                {
                    throw;
                }

                throw new Exception($"[ERROR] CreateChargeAlipayBscanC message: {ex.Message}");
            }
        }

        public async Task<ChargeArtChainResponseDto> CreateChargeArtChain(ChargeArtChainRequestDto request, Guid merchantId, string sourceName, Guid reqId)
        {
            try
            {
                // get jwt invalid
                var sourceId = request.SourceId;
                _logger.LogInformation($"PaymentService.CreateChargeArtChain sourceId: {sourceId} reqId: {reqId}");

                SourceRequestDto jwtInvalid = null;
                Guid jwtInvalidMerchantId = Guid.Empty;

                try
                {
                    jwtInvalid = await _jwtUtil.DecodeTokenWithoutValidate(sourceId);
                    jwtInvalidMerchantId = Guid.Parse(jwtInvalid.MerchantID);
                }
                catch (Exception)
                {
                    throw new TokenDecodeException();
                }

                var merchant = await _helperService.GetMerchantPaymentChannelMdrRate(jwtInvalidMerchantId, PaymentChannelType.ArtChainCtoB.Name);

                // get jwt valid
                // TODO: DUPLICATE Code, Considerate to refactor later
                var jwtValid = await _jwtUtil.DecodeToken(sourceId, merchant.SecretKey);
                await CheckDuplicateTransction(jwtValid.TransactionId);
                Guid jwtValidMerchantId = Guid.Parse(jwtValid.MerchantID);

                var payment = await CreatePayment(jwtValid.TransactionId, merchant.PaymentChannel, merchant.MdrRate, sourceName, request.Description, merchant.MerchantCode, merchant.MerchantName, merchant.IsCompany, jwtValid, merchant.MainBranchId, null);

                var transactionRepo = _unitOfWork.GetRepository<TransactionEntity>();
                var transactionList = await _transactionService.GetTransactionById(payment.Id);
                var transactionNo = transactionList.TransactionNo;
                var updateTransaction = transactionList;

                var qrTimeOut = await _paymentServiceInternal.GetQrTimeOut(request.qrTimeOut, 1, 10);

                try
                {
                    var requestDto = new RequestToArtChain
                    {
                        transactionNo = transactionNo,
                        merchantId = payment.billerId,
                        pointId = _appSettings.pointId,
                        amount = jwtValid.Amount,
                        secretKey = payment.secretApiKey,
                        qrTimeout = qrTimeOut,
                    };

                    var requestDtoStr = JsonSerializer.Serialize<RequestToArtChain>(requestDto);

                    var responseArtChain = await _artChainService.SendArtChainQrAsync("create_order", requestDtoStr, reqId);

                    updateTransaction.OrderId = responseArtChain.transactionNo.ToString();
                    updateTransaction.ChargeId = responseArtChain.trxId;

                    if (jwtValid.Description == "LinkToPay")
                    {
                        updateTransaction.InvoiceNo = transactionNo;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(updateTransaction.InvoiceNo))
                        {
                            updateTransaction.InvoiceNo = transactionNo;
                        }
                    }
                    var resultUpdate = await _transactionService.UpdateTransaction(updateTransaction);

                    return new ChargeArtChainResponseDto
                    {
                        respCode = "0000",
                        respDesc = "success",
                        transactionNo = transactionNo,
                        qrImage = responseArtChain.qrImage,
                        qrTimeOut = qrTimeOut,
                        pointName = responseArtChain.pointName,
                        walletId = responseArtChain.walletId
                    };
                }
                catch (Exception)
                {
                    // update transaction fail

                    await _paymentServiceInternal.updateTransactionFailAndSendNotify(updateTransaction, reqId);

                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR PaymentService.CreateChargeArtChain message: {ex.Message} reqId: {reqId}");

                if (ex is ICustomHttpException)
                {
                    throw;
                }

                throw new Exception($"[ERROR] CreateChargeArtChain message: {ex.Message}");
            }
        }

        public async Task<CancelResponseDto> CancelTransaction(CancelRequestDto request, Guid merchantId, Guid reqId)
        {
            try
            {
                _logger.LogDebug($"PaymentService.CancelTransaction request: {JsonSerializer.Serialize(request)} reqId: {reqId}");
                _logger.LogInformation($"PaymentService.CancelTransaction Transaction No: {request.TransactionNo} reqId: {reqId}");

                var merchantRepo = _unitOfWork.GetRepository<MerchantEntity>();
                var merchant = await merchantRepo.GetAll(false).Where(x => x.Id == merchantId && !x.IsDeleted)
                                        .FirstOrDefaultAsync();

                if (merchant == null)
                {
                    throw new MerchantIdNotFoundException();
                }

                var transactionRepo = _unitOfWork.GetRepository<TransactionEntity>();
                var transaction = await transactionRepo.GetAll(false).Where(x => x.MerchantId == merchantId
                                                                        && x.TransactionNo == request.TransactionNo
                                                                        && x.InvoiceNo == request.InvoiceNo
                                                                        && x.TransactionStatusId == TransactionStatus.WaitForTransfer
                                                                        && !x.Paid
                                                                        && !x.IsDeleted)
                                        .Select(x => x)
                                        .FirstOrDefaultAsync();

                if (transaction is null)
                {
                    throw new TransactionNotFoundToCancelException();
                }

                var bankCode = transaction.BankCode;
                var paymentChannel = transaction.PaymentChannel;

                var mBillerHeaderRepo = _unitOfWork.GetRepository<BillerHeaderEntity>();
                var mbiller = await mBillerHeaderRepo.GetAll().Where(x => x.MerchantId == merchantId && x.BankCode == bankCode && !x.IsDeleted && x.IsActive)
                                        .Select(x => new
                                        {
                                            x.SecretApiKey,
                                        })
                                        .FirstOrDefaultAsync();
                string SecretApiKey = "";

                if (mbiller != null)
                {
                    SecretApiKey = mbiller.SecretApiKey;
                }
                else
                {
                    var systemBillerHeaderRepo = _unitOfWork.GetRepository<SystemBillerHeaderEntity>();
                    var billerItem = await systemBillerHeaderRepo.GetAll().Where(x => x.BankCode == bankCode && !x.IsDeleted && x.IsActive)
                                        .Select(x => new { x.SecretApiKey })
                                        .FirstOrDefaultAsync();

                    SecretApiKey = billerItem.SecretApiKey;
                }

                var resource = new CancelKbankRequest
                {
                    transactionNo = transaction.TransactionNo,
                    QrId = transaction.ChargeId,
                    SecretKey = SecretApiKey,
                };
                string status = "success";

                if (bankCode == BankType.KBank.Name)
                {
                    if (transaction.PaymentChannel.ToLower() == PaymentChannelType.PromptPay.Name)
                    {
                        var bypassvalue = JsonSerializer.Serialize(resource, new JsonSerializerOptions()
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                        var result = await _kBankService.SendKBankCancelAsync("qr/cancel", bypassvalue);
                        //update order id to transaction 
                        if (result.status == "success")
                        {
                            var resultUpdate = await _transactionService.PaidCancel(transaction);
                        }
                        else
                        {
                            status = result.status;
                        }
                    }
                    else
                    {
                        var resultUpdate = await _transactionService.PaidCancel(transaction);
                    }
                }

                if (bankCode == BankType.SCB.Name)
                {
                    if (transaction.PaymentChannel.ToLower() == PaymentChannelType.WeChatPayCtoB.Name)
                    {
                        var merchantMdr = await _helperService.GetMerchantPaymentChannelMdrRate(transaction.MerchantId, PaymentChannelType.WeChatPayCtoB.Name);
                        var getBilerMdr = await GetBillerMdrForCreatePayment(PaymentChannelType.WeChatPayCtoB, merchantMdr.MdrRate, merchant.Id);

                        string SecretApiKey2 = "";
                        string ApiKey2 = "";
                        string TerminalId2 = "";

                        if (getBilerMdr != null)
                        {
                            SecretApiKey2 = getBilerMdr.SecretApiKey;
                            ApiKey2 = getBilerMdr.ApiKey;
                            TerminalId2 = getBilerMdr.TerminalId;
                        }

                        var resource2 = new CancelWeChatPayRequestDto
                        {
                            TransactionNo = request.TransactionNo,
                            MerchantId = transaction.MerchantId.ToString(),
                            TranType = "W",
                            CompanyId = _appSettings.SCBCompanyId,
                            TerminalId = TerminalId2,
                            OutTradeNo = _appSettings.isProduction ? request.TransactionNo : _appSettings.outTradeNoPrefix + request.TransactionNo,
                            ApiKey = ApiKey2,
                            SecretKey = SecretApiKey2
                        };

                        var bypassvalue = JsonSerializer.Serialize(resource2, new JsonSerializerOptions()
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        var result = await _scbService.SendSCBAlipayWeChatPayCancelAsync("alipaywechatpay/close", bypassvalue);

                        //update order id to transaction 
                        if (result.isSuccess)
                        {
                            var resultUpdate = await _transactionService.PaidCancel(transaction);
                        }
                        else
                        {
                            status = result.Description;
                        }
                    }
                    else
                    {
                        var resultUpdate = await _transactionService.PaidCancel(transaction);
                    }
                }
                else if (paymentChannel == PaymentChannelType.ArtChainCtoB.Name)
                {
                    var merchantMdr = await _helperService.GetMerchantPaymentChannelMdrRate(transaction.MerchantId, PaymentChannelType.ArtChainCtoB.Name);
                    var getBilerMdr = await GetBillerMdrForCreatePayment(PaymentChannelType.ArtChainCtoB, merchantMdr.MdrRate, merchant.Id);

                    var resource2 = new RequestCancelToArtChain
                    {
                        secretKey = getBilerMdr.SecretApiKey,
                        trxId = transaction.ChargeId
                    };
                    var bypassvalue = JsonSerializer.Serialize(resource2, new JsonSerializerOptions()
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    var result = await _artChainService.SendArtChainCancelAsync("cancel", bypassvalue);
                    //update order id to transaction 
                    if (result.Ok == true)
                    {
                        var resultUpdate = await _transactionService.PaidCancel(transaction);
                    }
                    else
                    {
                        status = result.Data;
                    }
                }
                else
                {
                    var resultUpdate = await _transactionService.PaidCancel(transaction);
                }



                return new CancelResponseDto
                {
                    respCode = "0000",
                    respDesc = status,
                    invoiceNo = request.InvoiceNo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR PaymentService.CancelTransaction message: {ex.Message} reqId: {reqId}");

                if (ex is ICustomHttpException)
                {
                    throw;
                }

                throw new Exception($"Fail to Cancel Transaction: {ex.Message}");
            }
        }


        #region Internal Service

        public async Task<GetBillerMdrForCreatePaymentResponseDto> GetBillerMdrForCreatePayment(PaymentChannelType paymentChannel, MerchantMdrRateResource mdrRateResource, Guid merchantId, decimal amount = 0)
        {
            if (mdrRateResource == null || !mdrRateResource.MdrRates.Any())
            {
                throw new Exception("MdrRates Not Found !!!");
            }
            //get merchant
            var merchantRepo = _unitOfWork.GetRepository<MerchantEntity>();
            var merchant = merchantRepo.GetAll().Where(a => a.Id == merchantId && !a.IsDeleted).FirstOrDefault() ?? throw new Exception("Merchant Is Null !!!");

            // declare for return value
            var BankCode = "";
            var abbv = "";
            var BillerId = "";
            decimal Mdr = 0;
            decimal MdrBank = 0;
            decimal MdrOwn = 0;
            decimal Margin = 0;

            var SecretApiKey = "";
            var PublicApiKey = "";
            var ApiKey = "";
            var TerminalId = "";
            var BankMerchantId = "";
            var ShopId = "";
            var PaymentChannelSecretApiKey = "";
            string CompCode = "";
            var PaymentChannelApiKey = "";

            _logger.LogInformation($"[INFO] GetBillerMdrForCreatePayment MerchantServiceType : {merchant.MerchantServiceType}");
            if (merchant.MerchantServiceType == 1) // type PF
            {
                // get biller id of system
                var systemBillerHeaderRepo = _unitOfWork.GetRepository<SystemBillerHeaderEntity>();
                var systemBilleritemRepo = _unitOfWork.GetRepository<SystemBillerItemEntity>();
                var billerHeaderRepo = _unitOfWork.GetRepository<BillerHeaderEntity>();
                var billerItemRepo = _unitOfWork.GetRepository<BillerItemEntity>();
                var accountRepo = _unitOfWork.GetRepository<AccountEntity>();

                List<SystemBillerHeaderAndItemDto> billers = new List<SystemBillerHeaderAndItemDto>();
                bool isSpecialCase = false;

                // 2 = Visa/Mastercard (QR)
                if (paymentChannel.Id == "2")
                {
                    // 025 Bay
                    var findBillerHeader = await billerHeaderRepo.GetAll().Where(x => x.MerchantId == merchant.Id && !x.IsDeleted && x.IsActive && x.BankCode == "025").FirstOrDefaultAsync();
                    if (findBillerHeader != null)
                    {
                        isSpecialCase = true;
                    }
                }

                // 10 = TrueMoney (C scan B - Offline)
                // 11 = TrueMoney (B scan C)
                // 18 = mobile banking kbank internal
                // 22 = mobile banking kbank external
                // 7 = Rabbit LINE Pay (C scan B)
                // 9 = Rabbit LINE Pay (B scan C)
                if (paymentChannel.Id == "10" || paymentChannel.Id == "11" || paymentChannel.Id == "18" || paymentChannel.Id == "22" || paymentChannel.Id == "7" || paymentChannel.Id == "9" || isSpecialCase)
                {
                    _logger.LogInformation($"[INFO] GetBillerMdrForCreatePayment: Get biller from merchant");
                    var billerQuery = billerHeaderRepo.GetAll().Where(x => x.MerchantId == merchant.Id && !x.IsDeleted && x.IsActive)
                                        .Join(billerItemRepo.GetAll().Where(x => x.PaymentChannelId == paymentChannel.Id && !x.IsDeleted && x.IsActive),
                                        a => a.Id,
                                        b => b.BillerHeaderId,
                                        (a, b) => new SystemBillerHeaderAndItemDto
                                        {
                                            Id = b.Id,
                                            abbv = b.Abbv,
                                            BillerId = b.BillerId,
                                            SecretApiKey = a.SecretApiKey,
                                            PublicApiKey = a.PublicApiKey,
                                            ApiKey = a.ApiKey,
                                            TerminalId = b.TerminalId,
                                            BankMerchantId = b.BankMerchantId,
                                            BankCode = a.BankCode,
                                            ShopId = b.ShopId,
                                            PaymentChannelSecretApiKey = b.PaymentChannelSecretApiKey,
                                            PaymentChannelApiKey = b.PaymentChannelApiKey,
                                            CompCode = b.CompCode
                                        });
                    billers = await billerQuery.ToListAsync();
                }
                else
                {
                    IQueryable<SystemBillerHeaderAndItemDto> systemBillerQuery = null;
                    _logger.LogInformation($"[INFO] GetBillerMdrForCreatePayment: Get biller from system");
                    if (paymentChannel.Id == "2")
                    {
                        systemBillerQuery = systemBillerHeaderRepo.GetAll().Where(x => !x.IsDeleted && x.IsActive && x.BankCode != "025")
                                                                   .OrderBy(x => x.Order)
                                                                   .Join(
                                                                           systemBilleritemRepo.GetAll().Where(x => x.PaymentChannelId == paymentChannel.Id && !x.IsDeleted && x.IsActive),
                                                                           a => a.Id,
                                                                           b => b.SystemBillerHeaderId,
                                                                           (a, b) => new SystemBillerHeaderAndItemDto
                                                                           {
                                                                               Id = b.Id,
                                                                               abbv = b.Abbv,
                                                                               BillerId = b.BillerId,
                                                                               SecretApiKey = a.SecretApiKey,
                                                                               PublicApiKey = a.PublicApiKey,
                                                                               ApiKey = a.ApiKey,
                                                                               TerminalId = b.TerminalId,
                                                                               BankMerchantId = b.BankMerchantId,
                                                                               BankCode = a.BankCode,
                                                                               ShopId = b.ShopId,
                                                                               PaymentChannelSecretApiKey = b.PaymentChannelSecretApiKey,
                                                                               PaymentChannelApiKey = b.PaymentChannelApiKey,

                                                                               CompCode = b.CompCode
                                                                           });
                    }
                    else
                    {
                        systemBillerQuery = systemBillerHeaderRepo.GetAll().Where(x => !x.IsDeleted && x.IsActive)
                                                                   .OrderBy(x => x.Order)
                                                                   .Join(
                                                                           systemBilleritemRepo.GetAll().Where(x => x.PaymentChannelId == paymentChannel.Id && !x.IsDeleted && x.IsActive),
                                                                           a => a.Id,
                                                                           b => b.SystemBillerHeaderId,
                                                                           (a, b) => new SystemBillerHeaderAndItemDto
                                                                           {
                                                                               Id = b.Id,
                                                                               abbv = b.Abbv,
                                                                               BillerId = b.BillerId,
                                                                               SecretApiKey = a.SecretApiKey,
                                                                               PublicApiKey = a.PublicApiKey,
                                                                               ApiKey = a.ApiKey,
                                                                               TerminalId = b.TerminalId,
                                                                               BankMerchantId = b.BankMerchantId,
                                                                               BankCode = a.BankCode,
                                                                               ShopId = b.ShopId,
                                                                               PaymentChannelSecretApiKey = b.PaymentChannelSecretApiKey,
                                                                               PaymentChannelApiKey = b.PaymentChannelApiKey,

                                                                               CompCode = b.CompCode
                                                                           });
                    }


                    billers = await systemBillerQuery.ToListAsync();
                }

                if (billers.Any())
                {
                    var sFind = billers.FirstOrDefault();

                    abbv = sFind.abbv;
                    BillerId = sFind.BillerId;
                    BankCode = sFind.BankCode;

                    SecretApiKey = sFind.SecretApiKey;
                    PublicApiKey = sFind.PublicApiKey;
                    ApiKey = sFind.ApiKey;
                    TerminalId = sFind.TerminalId;
                    BankMerchantId = sFind.BankMerchantId;
                    ShopId = sFind.ShopId;
                    PaymentChannelSecretApiKey = sFind.PaymentChannelSecretApiKey;
                    CompCode = sFind.CompCode;
                    PaymentChannelApiKey = sFind.PaymentChannelApiKey;


                    //get MDR
                    var getMdr = mdrRateResource.MdrRates.Where(a => a.BankCode == BankCode).FirstOrDefault();
                    if (getMdr != null && getMdr.Items.Any())
                    {
                        var getMdrRates = getMdr.Items.Where(a => a.PaymentChannelId == paymentChannel.Id).FirstOrDefault();
                        if (getMdrRates != null)
                        {
                            // 18 = Deep Link Kbank
                            if (paymentChannel.Id == "18" || paymentChannel.Id == "22")
                            {
                                if (getMdrRates.ConditionList.Any())
                                {
                                    var findMinAmount = getMdrRates.ConditionList.FirstOrDefault(x => x.Condition.Contains("Less Than Amount"));
                                    var findMaxAmount = getMdrRates.ConditionList.FirstOrDefault(x => x.Condition.Contains("More Than or Equal Amount"));
                                    if (amount < findMinAmount.Amount)
                                    {
                                        Mdr = findMinAmount.Bank + findMinAmount.Own;
                                        MdrBank = findMinAmount.Bank;
                                        MdrOwn = findMinAmount.Own;
                                        Margin = findMinAmount.Margin;
                                    }
                                    else
                                    {
                                        Mdr = findMaxAmount.Bank + findMaxAmount.Own;
                                        MdrBank = findMaxAmount.Bank;
                                        MdrOwn = findMaxAmount.Own;
                                        Margin = findMaxAmount.Margin;
                                    }
                                }
                                else
                                {
                                    throw new Exception("MdrRates - Mobile Banking (KBANK) Not Found !!!");
                                }
                            }
                            else
                            {
                                Mdr = getMdrRates.Bank + getMdrRates.Own;
                                MdrBank = getMdrRates.Bank;
                                MdrOwn = getMdrRates.Own;
                                Margin = getMdrRates.Margin;
                            }

                        }
                        else
                        {
                            throw new Exception("MdrRates Not Found !!!");
                        }
                    }
                    else
                    {
                        throw new Exception("MdrRates Not Found !!!");
                    }
                }
                else
                {
                    throw new Exception("Biller Not Found");
                }
            }
            else if (merchant.MerchantServiceType == 2) // type PPAS
            {
                // get biller id of merchant
                var mBillerHeaderRepo = _unitOfWork.GetRepository<BillerHeaderEntity>();
                var mBilleritemRepo = _unitOfWork.GetRepository<BillerItemEntity>();

                var mBillerQuery = mBillerHeaderRepo.GetAll().Where(x => x.MerchantId == merchantId && !x.IsDeleted && x.IsActive)
                                        .Join(
                                                mBilleritemRepo.GetAll().Where(x => x.PaymentChannelId == paymentChannel.Id && !x.IsDeleted && x.IsActive),
                                                a => a.Id,
                                                b => b.BillerHeaderId,
                                                (a, b) => new SystemBillerHeaderAndItemDto
                                                {
                                                    Id = b.Id,
                                                    abbv = b.Abbv,
                                                    BillerId = b.BillerId,
                                                    SecretApiKey = a.SecretApiKey,
                                                    PublicApiKey = a.PublicApiKey,
                                                    TerminalId = b.TerminalId,
                                                    BankMerchantId = b.BankMerchantId,
                                                    BankCode = a.BankCode,
                                                    ApiKey = a.ApiKey,
                                                    ShopId = b.ShopId,
                                                    PaymentChannelSecretApiKey = b.PaymentChannelSecretApiKey,
                                                    PaymentChannelApiKey = b.PaymentChannelApiKey,
                                                    CompCode = b.CompCode
                                                });

                var mBillers = await mBillerQuery.ToListAsync();
                if (mBillers.Any())
                {
                    var mBillerFirst = mBillers.FirstOrDefault();

                    abbv = mBillerFirst.abbv;
                    BillerId = mBillerFirst.BillerId;
                    BankCode = mBillerFirst.BankCode;

                    SecretApiKey = mBillerFirst.SecretApiKey;
                    PublicApiKey = mBillerFirst.PublicApiKey;
                    ApiKey = mBillerFirst.ApiKey;
                    TerminalId = mBillerFirst.TerminalId;
                    BankMerchantId = mBillerFirst.BankMerchantId;
                    ShopId = mBillerFirst.ShopId;
                    PaymentChannelSecretApiKey = mBillerFirst.PaymentChannelSecretApiKey;
                    PaymentChannelApiKey = mBillerFirst.PaymentChannelApiKey;
                    CompCode = mBillerFirst.CompCode;

                    var getMdr = mdrRateResource.MdrRates.Where(a => a.BankCode == mBillerFirst.BankCode).FirstOrDefault();
                    if (getMdr != null && getMdr.Items.Any())
                    {
                        var getMdrRates = getMdr.Items.Where(a => a.PaymentChannelId == paymentChannel.Id).FirstOrDefault();
                        if (getMdrRates != null)
                        {
                            Mdr = getMdrRates.Bank + getMdrRates.Own;
                            MdrBank = getMdrRates.Bank;
                            MdrOwn = getMdrRates.Own;
                            Margin = getMdrRates.Margin;
                        }
                        else
                        {
                            throw new Exception("MdrRates Not Found !!!");
                        }
                    }
                    else
                    {
                        throw new Exception("MdrRates Not Found !!!");
                    }
                }
                else
                {
                    throw new Exception("Biller Not Found");
                }
            }
            else
            {
                throw new Exception("Merchant Service Type Not Found !!!!");
            }

            if (!(
                paymentChannel == PaymentChannelType.TrueMoneyCtoBOffline ||
                paymentChannel == PaymentChannelType.TrueMoneyBtoC ||
                paymentChannel == PaymentChannelType.TrueMoneyCtoBOnline ||
                paymentChannel == PaymentChannelType.ArtChainCtoB ||
                paymentChannel == PaymentChannelType.MobileBankingKTBNext ||
                (paymentChannel == PaymentChannelType.PromptPay && BankCode == "025")
            ))
            {
                if (string.IsNullOrWhiteSpace(SecretApiKey))
                {
                    throw new SecretKeyNotFoundException();
                }
            }

            var result = new GetBillerMdrForCreatePaymentResponseDto
            {
                abbv = abbv,
                BillerId = BillerId,
                MdrBank = MdrBank,
                Margin = Margin,
                MdrOwn = MdrOwn,
                BankCode = BankCode,
                SecretApiKey = SecretApiKey,
                PublicApiKey = PublicApiKey,
                ApiKey = ApiKey,
                TerminalId = TerminalId,
                BankMerchantId = BankMerchantId,
                ShopId = ShopId,
                PaymentChannelSecretApiKey = PaymentChannelSecretApiKey,
                PaymentChannelApiKey = PaymentChannelApiKey,
                CompCode = CompCode
            };

            return result;
        }

        private async Task<PaymentDto> CreatePayment(string transactionId, PaymentChannelType paymentChannel, MerchantMdrRateResource mdrRateResource, string sourceName, string description, string merchantCode, string merchantName,
            bool isCompany, SourceWithKeyRequestDto jwtValid, Guid? MainBranchId, string deviceProfileId)
        {
            Guid jwtValidMerchantId = Guid.Parse(jwtValid.MerchantID);

            var getBilerMdr = await GetBillerMdrForCreatePayment(paymentChannel, mdrRateResource, jwtValidMerchantId, jwtValid.Amount);
            var getMerchantServiceType = await GetMerchantServiceTypeByMerchantId(jwtValidMerchantId);
            var getInternalOrder = await GetInternalOrder(jwtValidMerchantId, paymentChannel.Id);

            //save transaction
            var createDto = new TransactionDto
            {
                Abbv = getBilerMdr.abbv,
                Amount = jwtValid.Amount,
                BillerItemId = getBilerMdr.BillerId,
                MdrBank = getBilerMdr.MdrBank,
                MdrMargin = getBilerMdr.Margin,
                MdrOwn = getBilerMdr.MdrOwn,
                MerchantId = jwtValidMerchantId,
                MerchantCode = merchantCode,
                MerchantName = merchantName,
                Currency = jwtValid.Currency,
                Description = description,
                InvoiceNo = jwtValid.InvoiceNo,
                IsCompany = isCompany,
                PaymentChannel = paymentChannel.Name,
                SourceName = sourceName,
                BankCode = getBilerMdr.BankCode,
                MainBranchId = MainBranchId,
                DeviceProfileId = deviceProfileId,
                MerchantServiceType = getMerchantServiceType,
                InternalOrder = getInternalOrder
            };

            var transaction = await _transactionService.CreateTransaction(createDto, transactionId);

            return new PaymentDto
            {
                Id = transaction.Id,
                billerId = getBilerMdr.BillerId,
                secretApiKey = getBilerMdr.SecretApiKey,
                publicApiKey = getBilerMdr.PublicApiKey,
                bankCode = getBilerMdr.BankCode,
                Abbv = createDto.Abbv,
                apiKey = getBilerMdr.ApiKey,
                terminalId = getBilerMdr.TerminalId,
                bankMerchantId = getBilerMdr.BankMerchantId,
                shopId = getBilerMdr.ShopId,
                PaymentChannelSecretApiKey = getBilerMdr.PaymentChannelSecretApiKey,
                PaymentChannelApiKey = getBilerMdr.PaymentChannelApiKey,
                CompCode = getBilerMdr.CompCode
            };
        }

        public async Task<PaymentChannelDto> GetBankCodeFromMdrLowest(string paymentchannel, Guid merchantId)
        {
            var systemBillerHeaderRepo = _unitOfWork.GetRepository<SystemBillerHeaderEntity>();
            var mdrPaymentChannelRepo = _unitOfWork.GetRepository<MdrPaymentChannelEntity>();
            //getmdrrate
            var merchantRepo = _unitOfWork.GetRepository<MerchantEntity>();
            var merchantMdr = await merchantRepo.GetAll(false).Where(x => x.Id == merchantId && !x.IsDeleted)
                                    .Select(x => x.MdrRate)
                                    .FirstOrDefaultAsync();

            MerchantMdrRateResource merchantMdrRate = !string.IsNullOrWhiteSpace(merchantMdr)
                                            ? JsonSerializer.Deserialize<MerchantMdrRateResource>(merchantMdr)
                                            : new MerchantMdrRateResource();
            bool isMerchantMdr = false;

            if (merchantMdrRate.MdrRates != null)
            {
                if (merchantMdrRate.MdrRates?.Count > 0)
                {
                    var mdrMPayment = new MerchantMdr();
                    var merchantMdrRateItem = new DL.Merchants.MerchantMdrRateItem();

                    decimal mdrLowest = 100;

                    foreach (var mdrRate in merchantMdrRate.MdrRates)
                    {
                        bool isPaymentMdr = false;

                        foreach (var item in mdrRate.Items)
                        {
                            if (item.PaymentChannel.ToLower() == paymentchannel.ToLower())
                            {
                                merchantMdrRateItem = item;

                                isPaymentMdr = true;

                                if (mdrLowest >= item.Bank)
                                {
                                    mdrLowest = item.Bank;
                                    break;
                                }

                            }
                        }

                        if (isPaymentMdr)
                        {
                            mdrMPayment = mdrRate;
                            mdrMPayment.Items.Clear();
                            mdrMPayment.Items.Add(merchantMdrRateItem);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(mdrMPayment.BankCode))
                    {
                        isMerchantMdr = true;
                    }
                    else
                    {
                        var paymentChannelDto = new PaymentChannelDto
                        {
                            BankCode = mdrMPayment.BankCode,
                            PaymentChannelId = mdrMPayment.Items.FirstOrDefault().PaymentChannelId,
                            Mdr = mdrMPayment.Items.FirstOrDefault().Bank + mdrMPayment.Items.FirstOrDefault().Own,
                            MdrBank = (decimal)mdrMPayment.Items.FirstOrDefault().Bank,
                            MdrOwn = (decimal)mdrMPayment.Items.FirstOrDefault().Own,
                            Margin = (decimal)mdrMPayment.Items.FirstOrDefault().Margin
                        };

                        return paymentChannelDto;
                    }
                }
                else
                {
                    isMerchantMdr = true;
                }
            }
            else
            {
                isMerchantMdr = true;
            }

            // for unfinish uptask
            //bool isMerchantMdr = true;

            if (isMerchantMdr)
            {
                var mdrPaymentQuery = mdrPaymentChannelRepo.GetAll().Where(x => x.PaymentChannelName.ToLower() == paymentchannel.ToLower() && !x.IsDeleted)
                                        .Join(systemBillerHeaderRepo.GetAll().Where(x => !x.IsDeleted && x.IsActive),
                                                            a => a.BankCode,
                                                            b => b.BankCode,
                                                            (a, b) => new
                                                            {
                                                                a.BankCode,
                                                                a.PaymentChannelId,
                                                                a.Bank,
                                                                a.Own,
                                                                a.Margin,
                                                                b.Order
                                                            }).OrderBy(x => x.Bank).ThenBy(x => x.Order);

                var mdrPayment = await mdrPaymentQuery.FirstOrDefaultAsync();

                if (mdrPayment == null)
                {
                    throw new MdrPaymentNotFoundException();
                }

                var dto = new PaymentChannelDto
                {
                    BankCode = mdrPayment.BankCode,
                    PaymentChannelId = mdrPayment.PaymentChannelId,
                    Mdr = mdrPayment.Bank + mdrPayment.Own,
                    MdrBank = mdrPayment.Bank,
                    MdrOwn = mdrPayment.Own,
                    Margin = mdrPayment.Margin
                };

                return dto;
            }
            else
            {
                throw new Exception("Error No mdr");
            }
        }

        public async Task<int?> GetMerchantServiceTypeByMerchantId(Guid merchantId)
        {
            var merchantRepo = _unitOfWork.GetRepository<MerchantEntity>();
            var merchant = await merchantRepo.GetAll().Where(a => a.Id == merchantId && !a.IsDeleted).Select(a => new
            {
                a.Id,
                a.MerchantServiceType
            }).FirstOrDefaultAsync();
            return merchant != null ? merchant.MerchantServiceType : throw new Exception("Merchant Not Found !!!");
        }

        public async Task<TransactionEntity> GetTransaction(string transactionNo)
        {
            return await _transactionService.GetTransaction(transactionNo);
        }

        public async Task<string> GetInternalOrder(Guid merchantId, string paymentChanelCode)
        {
            var result = string.Empty;
            var merchantRepo = _unitOfWork.GetRepository<MerchantEntity>();
            var merchant = await merchantRepo.GetAll().Where(a => a.Id == merchantId && !a.IsDeleted).Select(a => new
            {
                a.CustomerGroup
            }).FirstOrDefaultAsync();

            if (merchant == null || string.IsNullOrEmpty(merchant.CustomerGroup))
            {
                throw new Exception("Merchant Not Found !!!");
            }

            var paymentChanelRepo = _unitOfWork.GetRepository<PaymentChannelsEntity>();
            var paymentChanels = await paymentChanelRepo.GetAll().Where(a => a.PaymentChannelCode == paymentChanelCode && a.IsActive).Select(a => new { a.ChannelInternalOrder }).FirstOrDefaultAsync();
            if (paymentChanels == null || string.IsNullOrEmpty(paymentChanels.ChannelInternalOrder))
            {
                throw new Exception("PaymentChanels Not Found !!!");
            }

            result = $"{_appSettings.ArgentoCompanyCode}{merchant.CustomerGroup}{_appSettings.ServiceGroupCode}{paymentChanels.ChannelInternalOrder}";

            return result;
        }

        private async Task CheckDuplicateTransction(string transactionId)
        {
            var transactionRepo = this._unitOfWork.GetRepository<TransactionEntity>();
            var transaction = await transactionRepo.GetAll().Where(x => x.Id == Guid.Parse(transactionId)).FirstOrDefaultAsync();

            if (transaction != null)
            {
                throw new TransactionDuplicateException();
            }
        }

        #endregion Internal Service
    }
}
