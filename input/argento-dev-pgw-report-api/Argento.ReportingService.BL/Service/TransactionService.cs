using Arcadia.Extensions.DependencyInjection.Attributes;
using Argento.ReportingService.BL.CustomHttpExceptions;
using Argento.ReportingService.BL.Interface;
using Argento.ReportingService.BL.Models;
using Argento.ReportingService.DL.Helpers;
using Argento.ReportingService.DL.Payments;
using Argento.ReportingService.DL.Reconciles;
using Argento.ReportingService.DL.Transactions;
using Argento.ReportingService.DL.Utils;
using Argento.ReportingService.Repository;
using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Repository.Model.ModelBase;
using Argento.ReportingService.Repository.ReportingServiceDB;
using Argento.ReportingService.Utility;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Npgsql;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static Argento.ReportingService.Utility.ArcadiaConstants;

namespace Argento.ReportingService.BL.Service
{
    [RegisterType(typeof(ITransactionService))]
    public class TransactionService : ITransactionService
    {
        private IUnitOfWorkReportingServiceDB unitOfWork;
        private readonly AppSettings appSettings;
        private IMapper mapper;
        private readonly DbContextReportingServiceDB _context;
        private readonly ILogger<TransactionService> _logger;

        private readonly EmailAttachment _emailAttachment;

        public TransactionService(IOptions<AppSettings> appSettings
            , IMapper mapper
            , IUnitOfWorkReportingServiceDB unitOfWork
            , DbContextReportingServiceDB context
            , ILogger<TransactionService> logger
            , EmailAttachment emailAttachment
        )
        {
            this.unitOfWork = unitOfWork;
            this.appSettings = appSettings.Value;
            this.mapper = mapper;
            this._context = context;
            _logger = logger;
            _emailAttachment = emailAttachment;
        }

        public async Task<TransactionEntity> UpdateTransaction(TransactionEntity updateDto)
        {
            using (IDbContextTransaction trx = unitOfWork.BeginDbContextTransaction())
            {
                try
                {
                    var transationRepo = unitOfWork.GetRepository<TransactionEntity>();
                    var existingData = await transationRepo.GetAll().Where(x => x.Id == updateDto.Id && !x.IsDeleted)
                        .Select(x => x)
                        .FirstOrDefaultAsync();
                    if (existingData == null)
                    {
                        throw new Exception("No Transaction data for update.");
                    }

                    await transationRepo.UpdateAsync(updateDto.MerchantId, updateDto);
                    await transationRepo.UnitOfWork.SaveChangesAsync();


                    trx.Commit();

                    return updateDto;
                }
                catch (Exception ex)
                {
                    trx.Rollback();

                    throw new Exception(ex.Message);
                }
            }
        }

        public async Task<List<TransactionEntity>> GetTransactionById(Guid id)
        {
            try
            {
                var transationRepo = unitOfWork.GetRepository<TransactionEntity>();
                var transaction = await transationRepo.GetAll().Where(x => x.Id == id && !x.IsDeleted)
                    .Select(x => x).ToListAsync();
                return transaction;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<TransactionListDto>> GetTransactionByRequest(TransactionRequestDto requestDto,
            Guid merchantId)
        {
            try
            {
                var transationRepo = unitOfWork.GetRepository<TransactionEntity>();
                var transaction = transationRepo.GetAll().Where(x => x.MerchantId == merchantId && !x.IsDeleted);

                if (!string.IsNullOrWhiteSpace(requestDto.StartDate))
                {
                    DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{requestDto.StartDate} 00:00:00", "yyyy-MM-dd HH:mm:ss");

                    transaction = transaction.Where(x => x.CreatedTimestamp >= startDate);
                }

                if (!string.IsNullOrWhiteSpace(requestDto.EndDate))
                {
                    DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{requestDto.EndDate} 23:59:59.997", "yyyy-MM-dd HH:mm:ss.fff");

                    transaction = transaction.Where(x => x.CreatedTimestamp <= endDate);
                }

                if (requestDto.PaymentChannels != null && requestDto.PaymentChannels.Any())
                {
                    var PaymentChannels = ConvertStringArrayOrStringSplitToListString(requestDto.PaymentChannels);

                    if (PaymentChannels.Count > 0)
                    {
                        if (PaymentChannels.Contains(PaymentChannelType.PromptPay.Name))
                        {
                            PaymentChannels.Add(PaymentChannelType.PromptPayFlatRate.Name);
                        }

                        transaction = transaction.Where(x => PaymentChannels.Contains(x.PaymentChannel.ToLower()));
                    }
                }

                if (requestDto.TransactionStatus != null && requestDto.TransactionStatus.Any())
                {
                    var TransactionStatus = ConvertStringArrayOrStringSplitToListString(requestDto.TransactionStatus);

                    if (TransactionStatus.Count > 0)
                    {
                        foreach (var status in TransactionStatus)
                        {
                            switch (status)
                            {
                                case "1":
                                case "2":
                                case "3":
                                case "4":
                                case "5":
                                    break;
                                default:
                                    throw new TransactionStatusMisMatchException();
                            }
                        }

                        transaction = transaction.Where(x =>
                            TransactionStatus.Contains(x.TransactionStatusId.ToString()));
                    }
                }

                transaction = transaction.OrderByDescending(x => x.CreatedTimestamp);

                var query = transaction.Select(x => new TransactionListDto
                {
                    TransactionNo = x.TransactionNo,
                    InvoiceNo = x.InvoiceNo,
                    TransactionDate =
                        CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(x.CreatedTimestamp,
                            "yyyy-MM-dd HH:mm:ss"),
                    TransactionStatusId = x.TransactionStatusId,
                    Amount = x.Amount,
                    paidAmount = x.PaidAmount,
                    Description = x.Description,
                    Fee = x.Fee,
                    FeeVat = x.FeeVat,
                    MerchantId = x.MerchantId,
                    MerchantName = x.MerchantName,
                    PaymentChannel = x.PaymentChannel,
                    Balance = x.Balance,
                    deviceProfileId = x.DeviceProfileId,
                });

                var result = await query.ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                if (ex is ICustomHttpException)
                {
                    throw;
                }

                throw new Exception(ex.Message);
            }
        }

        public async Task<TransactionResultPageDto> GetTransactionResult(Guid id)
        {
            try
            {
                var transationRepo = unitOfWork.GetRepository<TransactionEntity>();
                var transaction = transationRepo.GetAll().Where(x => x.Id == id && !x.IsDeleted);

                var query = transaction.Select(x => new TransactionResultPageDto
                {
                    Id = x.Id,
                    PayeeName = x.MerchantName,
                    CardNumber = x.CardMasking,
                    TransactionNo = x.TransactionNo,
                    InvoiceNo = x.InvoiceNo,
                    Amount = x.Amount,
                    PaymentDateTime =
                        CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(x.PaidAt, "dd/MM/yyyy HH:mm"),
                    IsSuccess = x.TransactionStatusId == TransactionStatus.Paid,
                    Result = x.PaidAt == null ? "Waiting" : (x.Paid ? "Approved" : "Rejected"),
                    ArgentoToken = "",
                });

                var result = await query.FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<PagedList<TransactionPagingDto>> GetTransactionByPaymentMerchant(Guid merchantId,
            TransactionPagingParameters resourceParameter)
        {
            try
            {
                // list merchantId of branch 
                var merchantRepo = unitOfWork.GetRepository<MerchantEntity>();
                var merchantsQuery = merchantRepo.GetAll().Where(x => x.MainBranchId == merchantId && !x.IsDeleted)
                    .Select(x => x.Id);

                var merchants = await merchantsQuery.ToListAsync();

                // add default merchantId to merchants
                merchants.Add(merchantId);

                var transationRepo = unitOfWork.GetRepository<TransactionEntity>();
                var transaction = transationRepo.GetAll().Where(x =>
                    merchants.Contains(x.MerchantId) && !x.IsDeleted
                );

                if (!string.IsNullOrWhiteSpace(resourceParameter.StartDate))
                {
                    DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{resourceParameter.StartDate} 00:00:00", "yyyy-MM-dd HH:mm:ss");

                    transaction = transaction.Where(x => x.CreatedTimestamp >= startDate);
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.EndDate))
                {
                    DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{resourceParameter.EndDate} 23:59:59.997", "yyyy-MM-dd HH:mm:ss.fff");

                    transaction = transaction.Where(x => x.CreatedTimestamp <= endDate);
                }

                if (resourceParameter.PaymentChannels != null && resourceParameter.PaymentChannels.Any())
                {
                    var PaymentChannels =
                        ConvertStringArrayOrStringSplitToListString(resourceParameter.PaymentChannels);

                    if (PaymentChannels.Count > 0)
                    {
                        if (PaymentChannels.Contains(PaymentChannelType.PromptPay.Name))
                        {
                            PaymentChannels.Add(PaymentChannelType.PromptPayFlatRate.Name);
                        }

                        if (PaymentChannels.Contains(PaymentChannelType.TrueMoneyCtoBOffline.Name) ||
                            PaymentChannels.Contains(PaymentChannelType.TrueMoneyCtoB.Name))
                        {
                            PaymentChannels.Remove(PaymentChannelType.TrueMoneyCtoBOffline.Name);
                            PaymentChannels.Remove(PaymentChannelType.TrueMoneyCtoB.Name);
                            PaymentChannels.Add(PaymentChannelType.TrueMoneyCtoBOnline.Name);
                            PaymentChannels.Add(PaymentChannelType.TrueMoneyCtoBOffline.Name);
                        }

                        transaction = transaction.Where(x => PaymentChannels.Contains(x.PaymentChannel.ToLower()));
                    }
                }

                if (resourceParameter.Sources != null && resourceParameter.Sources.Any())
                {
                    var Sources = ConvertStringArrayOrStringSplitToListString(resourceParameter.Sources);

                    if (Sources.Count > 0)
                    {
                        transaction = transaction.Where(x => Sources.Contains(x.SourceName.ToLower()));
                    }
                }

                if (resourceParameter.TransactionStatus != null && resourceParameter.TransactionStatus.Any())
                {
                    var TransactionStatus =
                        ConvertStringArrayOrStringSplitToListString(resourceParameter.TransactionStatus);

                    if (TransactionStatus.Count > 0)
                    {
                        transaction = transaction.Where(x =>
                            TransactionStatus.Contains(x.TransactionStatusId.ToString()));
                    }
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.Keyword))
                {
                    transaction = transaction.Where(x => x.MerchantName.Contains(resourceParameter.Keyword) ||
                                                         x.TransactionNo.Contains(resourceParameter.Keyword));
                }

                // order by transaction
                if (!string.IsNullOrWhiteSpace(resourceParameter.OrderBy.ToString()) &&
                    !string.IsNullOrWhiteSpace(resourceParameter.Order.ToString()))
                {
                    transaction = TransactionEntitySortable.CustomSort(transaction, resourceParameter.OrderBy,
                        resourceParameter.Order);
                }

                var result = transaction.Select(x => new TransactionPagingDto
                {
                    transactionId = x.Id,
                    MerchantId = x.MerchantId,
                    merchantName = x.MerchantName,
                    merchantCode = x.MerchantCode,
                    amount = Math.Round(x.Amount, 2),
                    paidAmount = x.PaidAmount,
                    transactionNo = x.TransactionNo,
                    InvoiceNo = x.InvoiceNo,
                    transactionDate =
                        CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(x.CreatedTimestamp,
                            "yyyy-MM-dd HH:mm:ss"),
                    paymentChannel =
                        (x.PaymentChannel == PaymentChannelType.TrueMoneyCtoBOffline.Name ||
                         x.PaymentChannel == PaymentChannelType.TrueMoneyCtoBOnline.Name
                            ? "truemoney (c scan b)"
                            : x.PaymentChannel),
                    fee = Math.Round(x.Fee, 2),
                    vat = Math.Round(x.FeeVat, 2),

                    netAmount = Math.Round(x.Balance, 2),
                    TransactionStatusId = x.TransactionStatusId,
                    transactionStatusName = TransactionStatus.FromStatusId(x.TransactionStatusId),
                    source = x.SourceName,
                    invoiceRef = x.InvoiceRef,
                    transferDateTime =
                        CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(x.TransferDateTime,
                            "yyyy-MM-dd HH:mm:ss"),
                    withHoldingTax = x.WithHoldingTax,
                    description = x.Description,
                    paidAt = CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(x.PaidAt,
                        "yyyy-MM-dd HH:mm:ss")
                });

                return await PagedList<TransactionPagingDto>.ToPagedList(result, resourceParameter.Page,
                    resourceParameter.PageSize);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<PagedList<TransactionPagingDto>> GetTransactionByPaymentAdmin(
            TransactionPagingParameters resourceParameter)
        {
            try
            {
                var transationRepo = unitOfWork.GetRepository<TransactionEntity>();
                var bankRepo = unitOfWork.GetRepository<BankEntity>();
                var transaction = transationRepo.GetAll().Where(x => !x.IsDeleted);

                if (!string.IsNullOrWhiteSpace(resourceParameter.StartDate))
                {
                    DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{resourceParameter.StartDate} 00:00:00", "yyyy-MM-dd HH:mm:ss");
                    _logger.LogInformation($"[INFO] Start Date : {startDate}");

                    transaction = transaction.Where(x => x.CreatedTimestamp >= startDate);
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.EndDate))
                {
                    DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{resourceParameter.EndDate} 23:59:59.997", "yyyy-MM-dd HH:mm:ss.fff");

                    transaction = transaction.Where(x => x.CreatedTimestamp <= endDate);
                }

                if (resourceParameter.PaymentChannels != null && resourceParameter.PaymentChannels.Any())
                {
                    var PaymentChannels =
                        ConvertStringArrayOrStringSplitToListString(resourceParameter.PaymentChannels);

                    if (PaymentChannels.Count > 0)
                    {
                        if (PaymentChannels.Contains(PaymentChannelType.PromptPay.Name))
                        {
                            PaymentChannels.Add(PaymentChannelType.PromptPayFlatRate.Name);
                        }

                        if (PaymentChannels.Contains(PaymentChannelType.TrueMoneyCtoB.Name))
                        {
                            PaymentChannels.Add(PaymentChannelType.TrueMoneyCtoBOffline.Name);
                            PaymentChannels.Add(PaymentChannelType.TrueMoneyCtoBOnline.Name);
                        }

                        transaction = transaction.Where(x => PaymentChannels.Contains(x.PaymentChannel.ToLower()));
                    }
                }

                if (resourceParameter.Sources != null && resourceParameter.Sources.Any())
                {
                    var Sources = ConvertStringArrayOrStringSplitToListString(resourceParameter.Sources);

                    if (Sources.Count > 0)
                    {
                        transaction = transaction.Where(x => Sources.Contains(x.SourceName.ToLower()));
                    }
                }

                if (resourceParameter.TransactionStatus != null && resourceParameter.TransactionStatus.Any())
                {
                    var TransactionStatus =
                        ConvertStringArrayOrStringSplitToListString(resourceParameter.TransactionStatus);

                    if (TransactionStatus.Count > 0)
                    {
                        transaction = transaction.Where(x =>
                            TransactionStatus.Contains(x.TransactionStatusId.ToString()));
                    }
                }

                if (resourceParameter.MerchantServiceTypes != null && resourceParameter.MerchantServiceTypes.Any())
                {
                    var MerchantServiceTypes =
                        ConvertStringArrayOrStringSplitToListString(resourceParameter.MerchantServiceTypes);

                    if (MerchantServiceTypes.Count > 0)
                    {
                        transaction = transaction.Where(x =>
                            MerchantServiceTypes.Contains(x.MerchantServiceType.ToString()));
                    }
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.Keyword))
                {
                    transaction = transaction.Where(x => x.MerchantName.Contains(resourceParameter.Keyword) ||
                                                         x.TransactionNo.Contains(resourceParameter.Keyword));
                }

                var totalData = await transaction.CountAsync();

                // order by transaction
                if (!string.IsNullOrWhiteSpace(resourceParameter.OrderBy.ToString()) &&
                    !string.IsNullOrWhiteSpace(resourceParameter.Order.ToString()))
                {
                    transaction = TransactionEntitySortable.CustomSort(transaction, resourceParameter.OrderBy,
                        resourceParameter.Order);
                }

                transaction = transaction.Skip((resourceParameter.Page - 1) * resourceParameter.PageSize)
                    .Take(resourceParameter.PageSize);

                var transactionQuery = transaction.Where(n => !n.IsDeleted)
                    .GroupJoin(_context.CustomMerchantEntity,
                        x => x.MainBranchId.HasValue ? x.MainBranchId : x.MerchantId,
                        y => y.Id,
                        (x, y) => new
                        {
                            t0 = x,
                            m0 = y,
                        })
                    .SelectMany(x => x.m0.DefaultIfEmpty(),
                        (x, y) => new
                        {
                            x.t0,
                            m0 = y,
                        })
                    .GroupJoin(_context.CustomMerchantEntity,
                        x => x.t0.MerchantId,
                        y => y.Id,
                        (x, y) => new
                        {
                            t1 = x,
                            m1 = y,
                        })
                    .SelectMany(x => x.m1.DefaultIfEmpty(),
                        (x, y) => new
                        {
                            t0 = x.t1,
                            m1 = y,
                        });


                var transactionBankQuery = from a in transactionQuery
                    join b in bankRepo.GetAll().Where(x => !x.IsDeleted) on a.t0.t0.BankCode equals b.BankCode into
                        joinedBank
                    from bank in joinedBank.DefaultIfEmpty()
                    select new TransactionPagingDto
                    {
                        transactionId = a.t0.t0.Id,
                        MerchantId = a.t0.t0.MerchantId,
                        merchantName = a.t0.t0.MerchantName,
                        merchantCode = a.t0.t0.MerchantCode,
                        mainBranchId = a.t0.m0.MerchantCode,
                        amount = Math.Round(a.t0.t0.Amount, 2),
                        paidAmount = a.t0.t0.PaidAmount,
                        transactionNo = a.t0.t0.TransactionNo,
                        InvoiceNo = a.t0.t0.InvoiceNo,
                        transactionDate =
                            CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(a.t0.t0.CreatedTimestamp,
                                "yyyy-MM-dd HH:mm:ss"),

                        paymentChannel =
                            a.t0.t0.PaymentChannel == PaymentChannelType.TrueMoneyCtoBOffline.Name ||
                            a.t0.t0.PaymentChannel == PaymentChannelType.TrueMoneyCtoBOnline.Name
                                ? "truemoney (c scan b)"
                                : a.t0.t0.PaymentChannel,
                        fee = Math.Round(a.t0.t0.Fee, 2),
                        vat = Math.Round(a.t0.t0.FeeVat, 2),

                        netAmount = Math.Round(a.t0.t0.Balance, 2),
                        TransactionStatusId = a.t0.t0.TransactionStatusId,
                        transactionStatusName = TransactionStatus.FromStatusId(a.t0.t0.TransactionStatusId),
                        source = a.t0.t0.SourceName,
                        invoiceRef = a.t0.t0.InvoiceRef,
                        sapCustomerId = a.m1.SapCustomerId,
                        merchantCategoryName = a.m1.MerchantCategoryName,
                        transferDateTime =
                            CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(a.t0.t0.TransferDateTime,
                                "yyyy-MM-dd"),
                        withHoldingTax = a.t0.t0.WithHoldingTax,
                        merchantServiceType = a.t0.t0.MerchantServiceType,
                        internalOrder = a.t0.t0.InternalOrder,
                        description = a.t0.t0.Description,
                        providerName = bank.BankName,
                        paidAt = CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(a.t0.t0.PaidAt,
                            "yyyy-MM-dd HH:mm:ss"),
                        chargeId = a.t0.t0.ChargeId,
                    };

                //get reconcile 
                var reconcileProcessRepo = unitOfWork.GetRepository<ReconcileProcessEntity>();
                var reconcileProcessDetailRepo = unitOfWork.GetRepository<ReconcileProcessDetailsEntity>();
                var settlementReportDetailsRepo = unitOfWork.GetRepository<SettlementReportDetailsEntity>();

                var reconcileData = reconcileProcessRepo.GetAll()
                    .Where(x => !x.IsDeleted && x.ProcessStatus == "Success")
                    .Join(bankRepo.GetAll().Where(x => !x.IsDeleted), a => a.Issuer, b => b.BankCode,
                        (a, b) => new
                        {
                            reconcileProcesses = a,
                            IssuerCode = b.BankCode,
                            IssuerName = b.BankName,
                            a.ReportFileName,
                        }).Join(reconcileProcessDetailRepo.GetAll().Where(x => !x.IsDeleted),
                        a => a.reconcileProcesses.Id, b => b.ReconcileProcessId,
                        (a, b) => new
                        {
                            issuer = a.IssuerName,
                            issuerCode = a.IssuerCode,
                            reconcileProcessId = a.reconcileProcesses.Id,
                            paymentmethod = b.PaymentMethod,
                            estimatedCashInDate =
                                CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(b.EstimatedCashInDate,
                                    "dd/MM/yyyy"),
                            reportFileName = a.ReportFileName,
                            reconcileReportNo = a.reconcileProcesses.ReportNo,
                        }).Join(settlementReportDetailsRepo.GetAll().Where(x => !x.IsDeleted),
                        a => a.reconcileProcessId, b => b.ReconcileProcessId,
                        (a, b) => new
                        {
                            paymentmethod = a.paymentmethod.ToLower(),
                            transactionNo = b.ReferenceOrder,
                            a.issuer,
                            a.issuerCode,
                            a.estimatedCashInDate,
                            bahtAmount = b.BahtAmount.ToString(),
                            bahtCommAmount = b.BahtCommAmount.ToString(),
                            bahtVAT = b.BahtVAT.ToString(),
                            WHT = b.WHT.ToString(),
                            bahtNetAmount = b.BahtNetAmount.ToString(),
                            bankReferenceOrder = b.BankReferenceOrder,
                            a.reportFileName,
                            authDateTime = b.AuthDateTime.HasValue
                                ? CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(b.AuthDateTime.Value,
                                    "yyyy-MM-dd HH:mm:ss")
                                : "",
                            a.reconcileReportNo,
                            BahtNetWHTAmount = b.BahtNetWHTAmount.ToString(),
                            test = b.BahtNetWHTAmount,
                        });
                transactionBankQuery = transactionBankQuery.GroupJoin(reconcileData,
                        a => new
                        {
                            a.transactionNo,
                            paymentChannel = (a.paymentChannel == "promptpay (flatrate)") ? "promptpay"
                                : a.paymentChannel.ToLower().Contains("alipay") ? "alipay"
                                : a.paymentChannel.ToLower().Contains("wechatpay") ? "wechatpay"
                                : a.paymentChannel.ToLower().Contains("truemoney") ? "truemoney"
                                : a.paymentChannel
                        },
                        b => new { b.transactionNo, paymentChannel = b.paymentmethod },
                        (a, b) => new { query = a, reconcile = b })
                    .SelectMany(x => x.reconcile.DefaultIfEmpty(),
                        (a, b) => new TransactionPagingDto
                        {
                            transactionId = a.query.transactionId,
                            MerchantId = a.query.MerchantId,
                            merchantName = a.query.merchantName,
                            merchantCode = a.query.merchantCode,
                            mainBranchId = a.query.mainBranchId,
                            amount = a.query.amount,
                            paidAmount = a.query.paidAmount,
                            transactionNo = a.query.transactionNo,
                            InvoiceNo = a.query.InvoiceNo,
                            transactionDate = a.query.transactionDate,
                            paymentChannel = a.query.paymentChannel,
                            fee = a.query.fee,
                            vat = a.query.vat,

                            netAmount = a.query.netAmount,
                            TransactionStatusId = a.query.TransactionStatusId,
                            transactionStatusName = a.query.transactionStatusName,
                            source = a.query.source,
                            invoiceRef = a.query.invoiceRef,
                            internalOrder = a.query.internalOrder,
                            sapCustomerId = a.query.sapCustomerId,
                            merchantCategoryName = a.query.merchantCategoryName,
                            transferDateTime = a.query.transferDateTime,
                            withHoldingTax = a.query.withHoldingTax,
                            issuer = b.issuer,
                            issuerCode = b.issuerCode,
                            estimatedCashInDate = b.estimatedCashInDate,
                            bahtAmount = decimal
                                .Round(decimal.Parse(b.bahtAmount ?? "0"), 2, MidpointRounding.AwayFromZero)
                                .ToString("#,##0.00"),
                            bahtCommAmount = decimal
                                .Round(decimal.Parse(b.bahtCommAmount ?? "0"), 2, MidpointRounding.AwayFromZero)
                                .ToString("#,##0.00"),
                            bahtVAT = decimal.Round(decimal.Parse(b.bahtVAT ?? "0"), 2, MidpointRounding.AwayFromZero)
                                .ToString("#,##0.00"),
                            bahtNetAmount = decimal
                                .Round(decimal.Parse(b.bahtNetAmount ?? "0"), 2, MidpointRounding.AwayFromZero)
                                .ToString("#,##0.00"),
                            wt = decimal.Round(decimal.Parse(b.WHT ?? "0"), 2, MidpointRounding.AwayFromZero)
                                .ToString("#,##0.00"),
                            wtNetAmount = decimal.Round(decimal.Parse(b.BahtNetWHTAmount ?? "0"), 2,
                                MidpointRounding.AwayFromZero).ToString("#,##0.00"),
                            description = a.query.description,
                            merchantServiceType = a.query.merchantServiceType,
                            bankReferenceOrder = b.bankReferenceOrder,
                            reportName = b.reportFileName,
                            authDateTime = b.authDateTime,
                            reconcileReportNo = b.reconcileReportNo,
                            providerName = a.query.providerName,
                            paidAt = a.query.paidAt,
                            chargeId = a.query.chargeId,
                        }).Select(x => x);

                // order by table relation
                if (!string.IsNullOrWhiteSpace(resourceParameter.OrderBy.ToString()) &&
                    !string.IsNullOrWhiteSpace(resourceParameter.Order.ToString()))
                {
                    transactionBankQuery = TransactionPagingDtoSortable.CustomSort(transactionBankQuery,
                        resourceParameter.OrderBy, resourceParameter.Order);
                }

                var resultData = await SetReturnDataByRole(transactionBankQuery.ToList(), resourceParameter.RoleId);
                var headerColumn = await SetReturnColumnByRole(resourceParameter.RoleId);

                return await PagedList<TransactionPagingDto>.ToPagedList(headerColumn, resultData,
                    resourceParameter.Page, resourceParameter.PageSize, totalData);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<PagedList<TransactionPagingDto>> GetTransactionByPaymentAdminTransfer(
            TransactionPagingParameters resourceParameter)
        {
            try
            {
                var transationRepo = unitOfWork.GetRepository<TransactionEntity>();
                var transaction = transationRepo.GetAll().Where(x =>
                    (x.TransactionStatusId == TransactionStatus.Paid ||
                     x.TransactionStatusId == TransactionStatus.NotFullyPaid) &&
                    !x.IsDeleted && x.MerchantServiceType == 1);

                if (!string.IsNullOrWhiteSpace(resourceParameter.StartDate))
                {
                    DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{resourceParameter.StartDate} 00:00:00", "yyyy-MM-dd HH:mm:ss");

                    transaction = transaction.Where(x => x.CreatedTimestamp >= startDate);
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.EndDate))
                {
                    DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{resourceParameter.EndDate} 23:59:59.997", "yyyy-MM-dd HH:mm:ss.fff");

                    transaction = transaction.Where(x => x.CreatedTimestamp <= endDate);
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.StartPaybackDate))
                {
                    DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{resourceParameter.StartPaybackDate} 00:00:00", "yyyy-MM-dd HH:mm:ss");

                    transaction = transaction.Where(x => x.TransferDateTime >= startDate);
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.EndPaybackDate))
                {
                    DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{resourceParameter.EndPaybackDate} 23:59:59.997", "yyyy-MM-dd HH:mm:ss.fff");

                    transaction = transaction.Where(x => x.TransferDateTime <= endDate);
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.StartEstimatedCashInDate))
                {
                    DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{resourceParameter.StartEstimatedCashInDate} 00:00:00", "yyyy-MM-dd HH:mm:ss");

                    transaction = transaction.Where(x => x.PaidAt >= startDate);
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.EndEstimatedCashInDate))
                {
                    DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{resourceParameter.EndEstimatedCashInDate} 23:59:59.997", "yyyy-MM-dd HH:mm:ss.fff");

                    transaction = transaction.Where(x => x.PaidAt <= endDate);
                }

                if (resourceParameter.PaymentChannels != null && resourceParameter.PaymentChannels.Any())
                {
                    var PaymentChannels =
                        ConvertStringArrayOrStringSplitToListString(resourceParameter.PaymentChannels);

                    if (PaymentChannels.Count > 0)
                    {
                        if (PaymentChannels.Contains(PaymentChannelType.PromptPay.Name))
                        {
                            PaymentChannels.Add(PaymentChannelType.PromptPayFlatRate.Name);
                        }

                        if (PaymentChannels.Contains(PaymentChannelType.TrueMoneyCtoB.Name))
                        {
                            PaymentChannels.Remove(PaymentChannelType.TrueMoneyCtoB.Name);
                            PaymentChannels.Add(PaymentChannelType.TrueMoneyCtoBOffline.Name);
                            PaymentChannels.Add(PaymentChannelType.TrueMoneyCtoBOnline.Name);
                        }

                        transaction = transaction.Where(x => PaymentChannels.Contains(x.PaymentChannel.ToLower()));
                    }
                }

                if (resourceParameter.Sources != null && resourceParameter.Sources.Any())
                {
                    var Sources = ConvertStringArrayOrStringSplitToListString(resourceParameter.Sources);

                    if (Sources.Count > 0)
                    {
                        transaction = transaction.Where(x => Sources.Contains(x.SourceName.ToLower()));
                    }
                }

                if (resourceParameter.TransactionStatus != null && resourceParameter.TransactionStatus.Any())
                {
                    var TransactionStatus =
                        ConvertStringArrayOrStringSplitToListString(resourceParameter.TransactionStatus);

                    if (TransactionStatus.Count > 0)
                    {
                        transaction = transaction.Where(x =>
                            TransactionStatus.Contains(x.TransactionStatusId.ToString()));
                    }
                }

                if (resourceParameter.Bank != null && resourceParameter.Bank.Any())
                {
                    var Bank = ConvertStringArrayOrStringSplitToListString(resourceParameter.Bank);

                    if (Bank.Count > 0)
                    {
                        transaction = transaction.Where(x => Bank.Contains(x.BankCode));
                    }
                }

                if (resourceParameter.ApproveStatus != null && resourceParameter.ApproveStatus.Any())
                {
                    var ApproveStatus = ConvertStringArrayOrStringSplitToListString(resourceParameter.ApproveStatus);

                    if (ApproveStatus.Count > 0 && !ApproveStatus.Any(x => x.ToLower() == "all"))
                    {
                        transaction = transaction.Where(x => ApproveStatus.Contains(x.ApproveStatusId.ToString()));
                    }
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.Keyword))
                {
                    transaction = transaction.Where(x => x.MerchantName.Contains(resourceParameter.Keyword) ||
                                                         x.TransactionNo.Contains(resourceParameter.Keyword));
                }

                // order by transaction
                if (!string.IsNullOrWhiteSpace(resourceParameter.OrderBy.ToString()) &&
                    !string.IsNullOrWhiteSpace(resourceParameter.Order.ToString()))
                {
                    transaction = TransactionEntitySortable.CustomSort(transaction, resourceParameter.OrderBy,
                        resourceParameter.Order);
                }

                var reconcileProcessRepo = unitOfWork.GetRepository<ReconcileProcessEntity>();
                var reconcileProcessDetailRepo = unitOfWork.GetRepository<ReconcileProcessDetailsEntity>();
                var settlementReportDetailsRepo = unitOfWork.GetRepository<SettlementReportDetailsEntity>();

                var fundingDetailRepo = unitOfWork.GetRepository<FundingDetailsEntity>();
                var fundingHeaderRepo = unitOfWork.GetRepository<FundingHeadersEntity>();
                var merchantRepo = unitOfWork.GetRepository<MerchantEntity>();
                var accountRepo = unitOfWork.GetRepository<AccountEntity>();

                var transactionQuery = transaction.Where(n => !n.IsDeleted)
                    .GroupJoin(_context.CustomMerchantEntity,
                        x => x.MainBranchId.HasValue ? x.MainBranchId : x.MerchantId,
                        y => y.Id,
                        (x, y) => new
                        {
                            t0 = x,
                            m0 = y,
                        })
                    .SelectMany(x => x.m0.DefaultIfEmpty(),
                        (x, y) => new
                        {
                            x.t0,
                            m0 = y,
                        })
                    .GroupJoin(_context.CustomMerchantEntity,
                        x => x.t0.MerchantId,
                        y => y.Id,
                        (x, y) => new
                        {
                            t1 = x,
                            m1 = y,
                        })
                    .SelectMany(x => x.m1.DefaultIfEmpty(),
                        (x, y) => new
                        {
                            t0 = x.t1,
                            m1 = y,
                        });
                var totalRecord = transactionQuery.Count();
                transactionQuery = transactionQuery.Skip((resourceParameter.Page - 1) * resourceParameter.PageSize)
                    .Take(resourceParameter.PageSize);

                var transactionTempQuery = transactionQuery
                    .Select(x => new TransactionTemp
                    {
                        transactionId = x.t0.t0.Id,
                        MerchantId = x.t0.t0.MerchantId,
                        merchantName = x.t0.t0.MerchantName,
                        merchantCode = x.t0.t0.MerchantCode,
                        mainBranchId = x.t0.m0.MerchantCode,
                        amount = Math.Round(x.t0.t0.Amount, 2),
                        paidAmount = x.t0.t0.PaidAmount,
                        transactionNo = x.t0.t0.TransactionNo,
                        InvoiceNo = x.t0.t0.InvoiceNo,
                        transactionDate =
                            CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(x.t0.t0.PaidAt,
                                "yyyy-MM-dd HH:mm:ss"),
                        paymentChannel = x.t0.t0.PaymentChannel,
                        fee = Math.Round(x.t0.t0.Fee, 2),
                        vat = Math.Round(x.t0.t0.FeeVat, 2),
                        mdr = Math.Round(x.t0.t0.Mdr, 2),

                        netAmount = Math.Round(x.t0.t0.Fee, 2) + Math.Round(x.t0.t0.FeeVat, 2),
                        transferAmount = x.t0.t0.Balance,
                        TransactionStatusId = x.t0.t0.TransactionStatusId,
                        transactionStatusName = TransactionStatus.FromStatusId(x.t0.t0.TransactionStatusId),
                        source = x.t0.t0.SourceName,
                        invoiceRef = x.t0.t0.InvoiceRef,
                        sapCustomerId = x.m1.SapCustomerId,
                        merchantCategoryName = x.m1.MerchantCategoryName,
                        payBackDate =
                            CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(x.t0.t0.TransferDateTime,
                                "yyyy-MM-dd HH:mm:ss"),
                        withHoldingTax = x.t0.t0.WithHoldingTax,
                        merchantServiceType = x.t0.t0.MerchantServiceType,
                        internalOrder = x.t0.t0.InternalOrder,
                        estimatedCashInDate =
                            CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(x.t0.t0.PaidAt,
                                "yyyy-MM-dd HH:mm:ss"),
                        fundingDetailId = x.t0.t0.FundingDetailId,
                        approveStatusId = x.t0.t0.ApproveStatusId,
                        bankCode = x.t0.t0.BankCode,
                    });


                transactionTempQuery = from tran in transactionTempQuery
                    join fundingDetailMain in fundingDetailRepo.GetAll().DefaultIfEmpty() on tran.fundingDetailId equals
                        fundingDetailMain.Id into fundingDetailJoin
                    from fundingDetail in fundingDetailJoin.DefaultIfEmpty()
                    select new TransactionTemp
                    {
                        transactionId = tran.transactionId,
                        MerchantId = tran.MerchantId,
                        merchantName = tran.merchantName,
                        merchantCode = tran.merchantCode,
                        mainBranchId = tran.mainBranchId,
                        amount = tran.amount,
                        paidAmount = tran.paidAmount,
                        transactionNo = tran.transactionNo,
                        InvoiceNo = tran.InvoiceNo,
                        transactionDate = tran.transactionDate,
                        paymentChannel = tran.paymentChannel,
                        fee = tran.fee,
                        vat = tran.vat,
                        mdr = tran.mdr,
                        transferAmount = tran.transferAmount,
                        netAmount = tran.netAmount,
                        TransactionStatusId = tran.TransactionStatusId,
                        transactionStatusName = tran.transactionStatusName,
                        source = tran.source,
                        invoiceRef = tran.invoiceRef,
                        sapCustomerId = tran.sapCustomerId,
                        merchantCategoryName = tran.merchantCategoryName,
                        payBackDate = tran.payBackDate,
                        withHoldingTax = tran.withHoldingTax,
                        merchantServiceType = tran.merchantServiceType,
                        internalOrder = tran.internalOrder,
                        estimatedCashInDate = tran.estimatedCashInDate,
                        fundingDetailId = tran.fundingDetailId,
                        fundingHeaderId = fundingDetail.FundingId,
                        approveStatusId = tran.approveStatusId,
                    };

                var resultData = from tran in transactionTempQuery
                    join fundingHeaderMain in fundingHeaderRepo.GetAll().DefaultIfEmpty() on tran.fundingHeaderId equals
                        fundingHeaderMain.Id into fundingHeaderJoin
                    from fundingHeader in fundingHeaderJoin.DefaultIfEmpty()
                    join merchantDetailMain in merchantRepo.GetAll().DefaultIfEmpty() on tran.MerchantId equals
                        merchantDetailMain.Id into merchantDetailJoin
                    from merchantDetail in merchantDetailJoin.DefaultIfEmpty()
                    join accountDetailMain in accountRepo.GetAll().Where(x => x.IsActive && !x.IsDeleted)
                        .DefaultIfEmpty() on tran.MerchantId equals accountDetailMain.MerchantId into accountDetailJoin
                    from accountDetail in accountDetailJoin.DefaultIfEmpty()
                    join settlement in settlementReportDetailsRepo.GetAll().Where(x => !x.IsDeleted).DefaultIfEmpty() on
                        tran.transactionNo equals settlement.ReferenceOrder into settlementJoin
                    from settlementMain in settlementJoin.DefaultIfEmpty()
                    join reconcile in reconcileProcessRepo.GetAll().Where(x =>
                            !x.IsDeleted && x.ProcessStatus.ToLower() == ReconcileStatus.Success.ToLower())
                        .DefaultIfEmpty() on settlementMain.ReconcileProcessId equals reconcile.Id into reconcileJoin
                    from reconcileMain in reconcileJoin.DefaultIfEmpty()
                    join reconcileDetail in reconcileProcessDetailRepo.GetAll().Where(x => !x.IsDeleted)
                            .DefaultIfEmpty()
                        on new
                        {
                            reconcileId = reconcileMain.Id,
                            paymentchanel = tran.paymentChannel.ToLower().Contains("alipay") ? "alipay"
                                : tran.paymentChannel.ToLower().Contains("wechatpay") ? "wechatpay"
                                : tran.paymentChannel.ToLower().Contains("truemoney") ? "truemoney"
                                : tran.paymentChannel.ToLower()
                        }
                        equals new
                        {
                            reconcileId = reconcileDetail.ReconcileProcessId,
                            paymentchanel = reconcileDetail.PaymentMethod.ToLower()
                        } into reconcileDetailJoin
                    from reconcileDetailMain in reconcileDetailJoin.DefaultIfEmpty()
                    select new TransactionPagingDto
                    {
                        transactionId = tran.transactionId,
                        MerchantId = tran.MerchantId,
                        merchantName = tran.merchantName,
                        merchantCode = tran.merchantCode,
                        mainBranchId = tran.mainBranchId,
                        amount = tran.amount,
                        paidAmount = tran.paidAmount,
                        transactionNo = tran.transactionNo,
                        InvoiceNo = tran.InvoiceNo,
                        transactionDate = tran.transactionDate,
                        paymentChannel = tran.paymentChannel,
                        fee = tran.fee,
                        vat = tran.vat,
                        mdr = tran.mdr,

                        netAmount = tran.netAmount,
                        transferAmount = tran.transferAmount,
                        TransactionStatusId = tran.TransactionStatusId,
                        transactionStatusName = tran.transactionStatusName,
                        source = tran.source,
                        invoiceRef = tran.invoiceRef,
                        sapCustomerId = tran.sapCustomerId,
                        merchantCategoryName = tran.merchantCategoryName,
                        payBackDate = tran.payBackDate,
                        withHoldingTax = tran.withHoldingTax,
                        merchantServiceType = tran.merchantServiceType,
                        internalOrder = tran.internalOrder,
                        estimatedCashInDate =
                            CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(
                                reconcileDetailMain.EstimatedCashInDate, "yyyy-MM-dd"),
                        fundingTransferReportNo = fundingHeader.FundingTransferReportNo,
                        paymentTerm = merchantDetail.PaymentTerm,
                        sapCompanyCode = tran.sapCustomerId,
                        accountName = accountDetail.AccountName,
                        accountNo = accountDetail.AccountNo,
                        bankName = accountDetail.BankName,
                        bankBranch = accountDetail.BankBranch,
                        reconcileReportNo = reconcileMain.ReportNo,
                        wtNetAmount = settlementMain.BahtNetWHTAmount > 0
                            ? decimal.Round(decimal.Parse(settlementMain.BahtNetWHTAmount.ToString() ?? "0"), 2,
                                MidpointRounding.AwayFromZero).ToString("#,##0.00")
                            : null,
                        approveStatusId = tran.approveStatusId == null ? 0 : tran.approveStatusId,
                        approveStatusName = tran.approveStatusId == null ? string.Empty :
                            tran.approveStatusId == 1 ? "Approve" :
                            tran.approveStatusId == 0 ? "Waiting" : "Reject"
                    };

                // order by table relation
                if (!string.IsNullOrWhiteSpace(resourceParameter.OrderBy.ToString()) &&
                    !string.IsNullOrWhiteSpace(resourceParameter.Order.ToString()))
                {
                    resultData = TransactionPagingDtoSortable.CustomSort(resultData, resourceParameter.OrderBy,
                        resourceParameter.Order);
                }

                return await PagedList<TransactionPagingDto>.ToPagedList(resultData, resourceParameter.Page,
                    resourceParameter.PageSize, totalRecord);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<TransactionDashboard> GetTransactionDashBoard(DashboardRequest resourceParameter)
        {
            try
            {
                TransactionDashboard transactionDashboard = new TransactionDashboard();
                // list merchantId of branch 
                var merchantRepo = unitOfWork.GetRepository<MerchantEntity>();
                var merchantsQuery = merchantRepo.GetAll().Where(x =>
                    (x.MainBranchId == resourceParameter.MerchantId || x.Id == resourceParameter.MerchantId) &&
                    !x.IsDeleted);
                var merchants = await merchantsQuery.Select(x => x.Id).ToListAsync();
                List<string> paymentList = new List<string>();
                var transationRepo = unitOfWork.GetRepository<TransactionEntity>();
                var transaction = transationRepo.GetAll().Where(x =>
                    merchants.Contains(x.MerchantId) && !x.IsDeleted
                );


                //before 7 day
                DateTime startdatetime = DateTime.UtcNow.Date.AddDays(-6).Date;
                transactionDashboard.DatetimePOC = startdatetime.ToString("yyyy-MM-dd") + " - " +
                                                   DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
                DateTime startDate = DateTime.UtcNow.Date.AddDays(-6);
                DateTime endDate = DateTime.UtcNow.Date.AddDays(1);
                // find only paid transaction today
                var transactionPaidToday = transaction.Where(x =>
                    x.Paid && x.CreatedTimestamp > DateTime.UtcNow.Date && x.CreatedTimestamp <= endDate);

                // get transaction from today and last 7 day
                transaction = transaction.Where(x => x.CreatedTimestamp > startDate && x.CreatedTimestamp <= endDate);
                //get payment list
                paymentList = await transaction.GroupBy(x => x.PaymentChannel)
                    .Select(g => g.Key).ToListAsync();

                paymentList.Remove("promptpay (flatrate)");
                paymentList.Remove(PaymentChannelType.TrueMoneyCtoBOnline.Name);
                paymentList.Remove(PaymentChannelType.TrueMoneyCtoBOffline.Name);

                var findTrueMoney = await transaction.Where(x =>
                    x.PaymentChannel == PaymentChannelType.TrueMoneyCtoBOffline.Name
                    || x.PaymentChannel == PaymentChannelType.TrueMoneyCtoBOnline.Name
                    || x.PaymentChannel == PaymentChannelType.TrueMoneyCtoB.Name
                    || x.PaymentChannel == PaymentChannelType.TrueMoneyBtoC.Name).ToListAsync();

                if (findTrueMoney.Any())
                {
                    paymentList.Add("truemoney (c scan b)");
                }

                transactionDashboard.PaymentChannel = paymentList.ToArray();
                // count all transaction
                transactionDashboard.AllTransaction = transaction.Count();
                // find only paid transaction
                var transactionPaid = transaction.Where(x => x.Paid);
                // count paid transaction
                transactionDashboard.AllTransactionPaid = transactionPaid.Count();
                // sum all paid amount
                var merchantMainQuery = merchantRepo.GetAll()
                    .Where(x => (x.Id == resourceParameter.MerchantId) && !x.IsDeleted).Select(x => x.Id).ToListAsync();
                if (merchantMainQuery.Result.Count() > 0)
                {
                    transactionDashboard.AllPaidAmount = await transactionPaid.GroupBy(x => x.Paid)
                        .Select(g => g.Sum(s => s.Amount)).FirstOrDefaultAsync();
                }
                else
                {
                    transactionDashboard.AllPaidAmount = await transactionPaid.GroupBy(x => x.MerchantId)
                        .Select(g => g.Sum(s => s.Amount)).FirstOrDefaultAsync();
                }

                // sum all paid amount today
                transactionDashboard.AllPaidAmountToday = await transactionPaidToday.GroupBy(x => x.MerchantId)
                    .Select(g => g.Sum(s => s.Amount)).FirstOrDefaultAsync();

                if (resourceParameter.PaymentChannel != "" && resourceParameter.PaymentChannel != null)
                {
                    if (resourceParameter.PaymentChannel.ToLower() == "truemoney (c scan b)")
                    {
                        transactionPaid = transactionPaid.Where(x =>
                            x.PaymentChannel.Contains(resourceParameter.PaymentChannel.ToLower()
                                .Remove(resourceParameter.PaymentChannel.Length - 1)));
                    }
                    else
                        transactionPaid = transactionPaid.Where(x =>
                            x.PaymentChannel.Contains(resourceParameter.PaymentChannel.ToLower()));
                }
                else
                {
                    if (transactionDashboard.PaymentChannel.Count() > 0)
                    {
                        if (transactionDashboard.PaymentChannel[0].ToLower() == "truemoney (c scan b)")
                        {
                            transactionPaid = transactionPaid.Where(x =>
                                x.PaymentChannel.Contains(transactionDashboard.PaymentChannel[0].ToLower()
                                    .Remove(transactionDashboard.PaymentChannel[0].Length - 1)));
                        }
                        else
                            transactionPaid = transactionPaid.Where(x =>
                                x.PaymentChannel.Contains(transactionDashboard.PaymentChannel[0].ToLower()));
                    }
                }

                var transactionDate = transactionPaid.Select(g => new
                {
                    Amount = g.Amount,
                    PaidAt = g.PaidAt.Value.AddHours(7).Date,
                });
                var items = await transactionDate.ToListAsync();
                var transactionGroupDate = items.GroupBy(x => x.PaidAt)
                    .Select(g => new
                    {
                        Key = g.Key,
                        Value = g.Sum(s => s.Amount),
                        Date = g.First().PaidAt
                    });
                //create Line Chart
                transactionDashboard.DateLine = new string[7];
                transactionDashboard.DateLineValue = new decimal[7];
                decimal dateValue = 0;
                for (int i = 0; i < 7; i++)
                {
                    var amount = transactionGroupDate.Where(s => s.Date == startdatetime).Select(x => x.Value)
                        .FirstOrDefault();
                    transactionDashboard.DateLine[i] = startdatetime.ToString("dd MMM yyyy");
                    transactionDashboard.DateLineValue[i] = amount;
                    dateValue += amount;
                    startdatetime = startdatetime.AddDays(1);
                }

                transactionDashboard.AllDateLineValue = dateValue;
                transactionDashboard.PaymentChannelValue = new decimal[paymentList.Count()];
                transactionDashboard.PaymentChannelValuePerCent = new decimal[paymentList.Count()];
                var updateTrans = transactionPaidToday.ToList();
                foreach (var u in updateTrans)
                {
                    if (u.PaymentChannel == "promptpay (flatrate)")
                        u.PaymentChannel = "promptpay";
                    if (u.PaymentChannel == PaymentChannelType.TrueMoneyCtoBOffline.Name ||
                        u.PaymentChannel == PaymentChannelType.TrueMoneyCtoBOnline.Name)
                        u.PaymentChannel = "truemoney (c scan b)";
                }

                var transactionPayment = updateTrans.GroupBy(x => x.PaymentChannel)
                    .Select(g => new
                    {
                        Key = g.Key,
                        Value = g.Sum(s => s.Amount),
                        PaymentChannel = g.First().PaymentChannel
                    });
                int k = 0;
                decimal allamount = 0;

                allamount = transactionDashboard.AllPaidAmountToday == 0 ? 1 : transactionDashboard.AllPaidAmountToday;

                foreach (string paymentc in paymentList)
                {
                    var amount = transactionPayment.Where(p => p.PaymentChannel.Contains(paymentc.ToLower()))
                        .Select(x => x.Value).FirstOrDefault();
                    transactionDashboard.PaymentChannelValue[k] = amount;
                    transactionDashboard.PaymentChannelValuePerCent[k] = Math.Round((amount / allamount) * 100, 2);
                    k++;
                }

                return transactionDashboard;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public TransactionListDto GetTransactionByTransactionNo(Guid merchantId, string transactionNo)
        {
            if (string.IsNullOrWhiteSpace(transactionNo))
            {
                throw new TransactionNotFoundException();
            }

            var transationRepo = unitOfWork.GetRepository<TransactionEntity>();
            var transaction = transationRepo.GetAll().Where(x =>
                x.MerchantId == merchantId && x.TransactionNo == transactionNo && !x.IsDeleted);

            var result = transaction.Select(x => new TransactionListDto
            {
                TransactionNo = x.TransactionNo,
                InvoiceNo = x.InvoiceNo,
                TransactionDate =
                    CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(x.CreatedTimestamp, "yyyy-MM-dd HH:mm:ss"),
                TransactionStatusId = x.TransactionStatusId,
                Amount = x.Amount,
                paidAmount = x.PaidAmount,
                Description = x.Description,
                Fee = x.Fee,
                FeeVat = x.FeeVat,
                MerchantId = x.MerchantId,
                MerchantName = x.MerchantName,
                PaymentChannel = x.PaymentChannel,
                Balance = x.Balance,
                deviceProfileId = x.DeviceProfileId,
            }).FirstOrDefault();

            if (result == null)
            {
                throw new TransactionNotFoundException();
            }

            return result;
        }

        public async ValueTask SaveData(TransactionEntity dto)
        {
            string commandText =
                $@"insert into dbo.""Transaction"" (
""Id"",

""CreatedByUserId"",
""CreatedTimestamp"",
""LastModifiedByUserId"",
""LastModifiedTimestamp"",
""DeletedByUserId"", 
""DeletedTimestamp"",
""IsDeleted"",

""MerchantId"",
""MerchantName"",
""TransactionNo"",
""PaymentChannel"",
""Amount"",

""Mdr"",
""Fee"",
""FeeVat"",
""Vat"",
""Balance"",

""Paid"",
""PaidAt"",
""PaidCode"",
""PaidMessage"",
""TransactionStatusId"",

""Reference1"",
""Reference2"",
""Reference3"",
""Description"",
""ChargeId"",

""OrderId"",
""CardMasking"",
""InvoiceNo"",
""SourceName"",
""ApproveStatusId"",

""TransferAmount"",
""TransferDateTime"",
""TransferStatusId"",
""MerchantCode"",
""InvoiceRef"",

""BankCode"",
""MainBranchId"",
""CancelAt"",
""WithHoldingTax"",
""DeviceProfileId"",

""PaidAmount"",
""MerchantServiceType"",
""InternalOrder""
)
values (
@Id,

@CreatedByUserId,
@CreatedTimestamp,
@LastModifiedByUserId,
@LastModifiedTimestamp,
@DeletedByUserId,
@DeletedTimestamp,
@IsDeleted,

@MerchantId,
@MerchantName,
@TransactionNo,
@PaymentChannel,
@Amount,

@Mdr,
@Fee,
@FeeVat,
@Vat,
@Balance,

@Paid,
@PaidAt,
@PaidCode,
@PaidMessage,
@TransactionStatusId,

@Reference1,
@Reference2,
@Reference3,
@Description,
@ChargeId,

@OrderId,
@CardMasking,
@InvoiceNo,
@SourceName,
@ApproveStatusId,

@TransferAmount,
@TransferDateTime,
@TransferStatusId,
@MerchantCode,
@InvoiceRef,

@BankCode,
@MainBranchId,
@CancelAt,
@WithHoldingTax,
@DeviceProfileId,

@PaidAmount,
@MerchantServiceType,
@InternalOrder
)
ON CONFLICT (""Id"")
do
    update set 
""CreatedByUserId""=@CreatedByUserId,
""CreatedTimestamp""=@CreatedTimestamp,
""LastModifiedByUserId""=@LastModifiedByUserId,
""LastModifiedTimestamp""=@LastModifiedTimestamp,
""DeletedByUserId""=@DeletedByUserId, 
""DeletedTimestamp""=@DeletedTimestamp,
""IsDeleted""=@IsDeleted,

""MerchantId""=@MerchantId,
""MerchantName""=@MerchantName,
""TransactionNo""=@TransactionNo,
""PaymentChannel""=@PaymentChannel,
""Amount""=@Amount,

""Mdr""=@Mdr,
""Fee""=@Fee,
""FeeVat""=@FeeVat,
""Vat""=@Vat,
""Balance""=@Balance,

""Paid""=@Paid,
""PaidAt""=@PaidAt,
""PaidCode""=@PaidCode,
""PaidMessage""=@PaidMessage,
""TransactionStatusId""=@TransactionStatusId,

""Reference1""=@Reference1,
""Reference2""=@Reference2,
""Reference3""=@Reference3,
""Description""=@Description,
""ChargeId""=@ChargeId,

""OrderId""=@OrderId,
""CardMasking""=@CardMasking,
""InvoiceNo""=@InvoiceNo,
""SourceName""=@SourceName,
""ApproveStatusId""=@ApproveStatusId,

""TransferAmount""=@TransferAmount,
""TransferDateTime""=@TransferDateTime,
""TransferStatusId""=@TransferStatusId,
""MerchantCode""=@MerchantCode,
""InvoiceRef""=@InvoiceRef,

""BankCode""=@BankCode,
""MainBranchId""=@MainBranchId,
""CancelAt""=@CancelAt,
""WithHoldingTax""=@WithHoldingTax,
""DeviceProfileId""=@DeviceProfileId,

""PaidAmount""=@PaidAmount,
""MerchantServiceType""=@MerchantServiceType,
""InternalOrder""=@InternalOrder
";
            await using NpgsqlConnection conn = new(appSettings.ConnectionStrings.DefaultConnection);
            await conn.OpenAsync();
            await using NpgsqlCommand cmd = new(commandText, conn);

            cmd.Parameters.AddWithValue("Id", dto.Id);

            cmd.Parameters.AddWithValue("CreatedByUserId", dto.CreatedByUserId);
            cmd.Parameters.AddWithValue("CreatedTimestamp", dto.CreatedTimestamp);
            cmd.Parameters.AddWithValue("LastModifiedByUserId", dto.LastModifiedByUserId);
            cmd.Parameters.AddWithValue("LastModifiedTimestamp", dto.LastModifiedTimestamp);
            cmd.Parameters.AddWithValue("DeletedByUserId",
                dto.DeletedByUserId.HasValue ? dto.DeletedByUserId : DBNull.Value);
            cmd.Parameters.AddWithValue("DeletedTimestamp",
                dto.DeletedTimestamp.HasValue ? dto.DeletedTimestamp : DBNull.Value);
            cmd.Parameters.AddWithValue("IsDeleted", dto.IsDeleted);

            cmd.Parameters.AddWithValue("MerchantId", dto.MerchantId);
            cmd.Parameters.AddWithValue("MerchantName", dto.MerchantName);
            cmd.Parameters.AddWithValue("TransactionNo", dto.TransactionNo);
            cmd.Parameters.AddWithValue("PaymentChannel", dto.PaymentChannel);
            cmd.Parameters.AddWithValue("Amount", dto.Amount);

            cmd.Parameters.AddWithValue("Mdr", dto.Mdr);
            cmd.Parameters.AddWithValue("Fee", dto.Fee);
            cmd.Parameters.AddWithValue("FeeVat", dto.FeeVat);
            cmd.Parameters.AddWithValue("Vat", dto.Vat);
            cmd.Parameters.AddWithValue("Balance", dto.Balance);

            cmd.Parameters.AddWithValue("Paid", dto.Paid);
            cmd.Parameters.AddWithValue("PaidAt", dto.PaidAt.HasValue ? dto.PaidAt : DBNull.Value);
            cmd.Parameters.AddWithValue("PaidCode",
                string.IsNullOrWhiteSpace(dto.PaidCode) ? DBNull.Value : dto.PaidCode);
            cmd.Parameters.AddWithValue("PaidMessage",
                string.IsNullOrWhiteSpace(dto.PaidMessage) ? DBNull.Value : dto.PaidMessage);
            cmd.Parameters.AddWithValue("TransactionStatusId", dto.TransactionStatusId);

            cmd.Parameters.AddWithValue("Reference1", dto.Reference1);
            cmd.Parameters.AddWithValue("Reference2",
                string.IsNullOrWhiteSpace(dto.Reference2) ? DBNull.Value : dto.Reference2);
            cmd.Parameters.AddWithValue("Reference3",
                string.IsNullOrWhiteSpace(dto.Reference3) ? DBNull.Value : dto.Reference3);
            cmd.Parameters.AddWithValue("Description",
                string.IsNullOrWhiteSpace(dto.Description) ? DBNull.Value : dto.Description);
            cmd.Parameters.AddWithValue("ChargeId",
                string.IsNullOrWhiteSpace(dto.ChargeId) ? DBNull.Value : dto.ChargeId);

            cmd.Parameters.AddWithValue("OrderId", string.IsNullOrWhiteSpace(dto.OrderId) ? DBNull.Value : dto.OrderId);
            cmd.Parameters.AddWithValue("CardMasking",
                string.IsNullOrWhiteSpace(dto.CardMasking) ? DBNull.Value : dto.CardMasking);
            cmd.Parameters.AddWithValue("InvoiceNo",
                string.IsNullOrWhiteSpace(dto.InvoiceNo) ? DBNull.Value : dto.InvoiceNo);
            cmd.Parameters.AddWithValue("SourceName", dto.SourceName);
            cmd.Parameters.AddWithValue("ApproveStatusId", dto.ApproveStatusId);

            cmd.Parameters.AddWithValue("TransferAmount",
                dto.TransferAmount.HasValue ? dto.TransferAmount : DBNull.Value);
            cmd.Parameters.AddWithValue("TransferDateTime",
                dto.TransferDateTime.HasValue ? dto.TransferDateTime : DBNull.Value);
            cmd.Parameters.AddWithValue("TransferStatusId",
                dto.TransferStatusId.HasValue ? dto.TransferStatusId : DBNull.Value);
            cmd.Parameters.AddWithValue("MerchantCode", dto.MerchantCode);
            cmd.Parameters.AddWithValue("InvoiceRef",
                string.IsNullOrWhiteSpace(dto.InvoiceRef) ? DBNull.Value : dto.InvoiceRef);

            cmd.Parameters.AddWithValue("BankCode", dto.BankCode);
            cmd.Parameters.AddWithValue("MainBranchId", dto.MainBranchId.HasValue ? dto.MainBranchId : DBNull.Value);
            cmd.Parameters.AddWithValue("CancelAt", dto.CancelAt.HasValue ? dto.CancelAt : DBNull.Value);
            cmd.Parameters.AddWithValue("WithHoldingTax", dto.WithHoldingTax);
            cmd.Parameters.AddWithValue("DeviceProfileId",
                string.IsNullOrWhiteSpace(dto.DeviceProfileId) ? DBNull.Value : dto.DeviceProfileId);

            cmd.Parameters.AddWithValue("PaidAmount", dto.PaidAmount.HasValue ? dto.PaidAmount : DBNull.Value);
            cmd.Parameters.AddWithValue("MerchantServiceType",
                dto.MerchantServiceType.HasValue ? dto.MerchantServiceType : DBNull.Value);
            cmd.Parameters.AddWithValue("InternalOrder",
                string.IsNullOrWhiteSpace(dto.InternalOrder) ? DBNull.Value : dto.InternalOrder);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<byte[]> GetTransactionByPaymentAdminExcel(TransactionPagingParameters resourceParameter)
        {
            try
            {
                _logger.LogInformation(
                    $"[INFO] GetTransactionByPaymentAdminExcel Start : {JsonConvert.SerializeObject(resourceParameter)}");
                var transationRepo = unitOfWork.GetRepository<TransactionEntity>();
                var bankRepo = unitOfWork.GetRepository<BankEntity>();
                var transaction = transationRepo.GetAll().Where(x => !x.IsDeleted);

                #region | Filter |

                if (!string.IsNullOrWhiteSpace(resourceParameter.StartDate))
                {
                    DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{resourceParameter.StartDate} 00:00:00", "yyyy-MM-dd HH:mm:ss");

                    transaction = transaction.Where(x => x.CreatedTimestamp >= startDate);
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.EndDate))
                {
                    DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{resourceParameter.EndDate} 23:59:59.997", "yyyy-MM-dd HH:mm:ss.fff");

                    transaction = transaction.Where(x => x.CreatedTimestamp <= endDate);
                }

                if (resourceParameter.PaymentChannels != null && resourceParameter.PaymentChannels.Any())
                {
                    var PaymentChannels =
                        ConvertStringArrayOrStringSplitToListString(resourceParameter.PaymentChannels);

                    if (PaymentChannels.Count > 0)
                    {
                        if (PaymentChannels.Contains(PaymentChannelType.PromptPay.Name))
                        {
                            PaymentChannels.Add(PaymentChannelType.PromptPayFlatRate.Name);
                        }

                        if (PaymentChannels.Contains(PaymentChannelType.TrueMoneyCtoB.Name))
                        {
                            PaymentChannels.Add(PaymentChannelType.TrueMoneyCtoBOffline.Name);
                            PaymentChannels.Add(PaymentChannelType.TrueMoneyCtoBOnline.Name);
                        }

                        transaction = transaction.Where(x => PaymentChannels.Contains(x.PaymentChannel.ToLower()));
                    }
                }

                if (resourceParameter.Sources != null && resourceParameter.Sources.Any())
                {
                    var Sources = ConvertStringArrayOrStringSplitToListString(resourceParameter.Sources);

                    if (Sources.Count > 0)
                    {
                        transaction = transaction.Where(x => Sources.Contains(x.SourceName.ToLower()));
                    }
                }

                if (resourceParameter.TransactionStatus != null && resourceParameter.TransactionStatus.Any())
                {
                    var TransactionStatus =
                        ConvertStringArrayOrStringSplitToListString(resourceParameter.TransactionStatus);

                    if (TransactionStatus.Count > 0)
                    {
                        transaction = transaction.Where(x =>
                            TransactionStatus.Contains(x.TransactionStatusId.ToString()));
                    }
                }

                if (resourceParameter.MerchantServiceTypes != null && resourceParameter.MerchantServiceTypes.Any())
                {
                    var MerchantServiceTypes =
                        ConvertStringArrayOrStringSplitToListString(resourceParameter.MerchantServiceTypes);

                    if (MerchantServiceTypes.Count > 0)
                    {
                        transaction = transaction.Where(x =>
                            MerchantServiceTypes.Contains(x.MerchantServiceType.ToString()));
                    }
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.Keyword))
                {
                    transaction = transaction.Where(x => x.MerchantName.Contains(resourceParameter.Keyword) ||
                                                         x.TransactionNo.Contains(resourceParameter.Keyword));
                }

                #endregion

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                #region | Query |

                var query1 = transaction.Where(n => !n.IsDeleted)
                    .GroupJoin(_context.CustomMerchantEntity,
                        x => x.MainBranchId.HasValue ? x.MainBranchId : x.MerchantId,
                        y => y.Id,
                        (x, y) => new
                        {
                            t0 = x,
                            m0 = y,
                        })
                    .SelectMany(x => x.m0.DefaultIfEmpty(),
                        (x, y) => new
                        {
                            x.t0,
                            m0 = y,
                        })
                    .GroupJoin(_context.CustomMerchantEntity,
                        x => x.t0.MerchantId,
                        y => y.Id,
                        (x, y) => new
                        {
                            t1 = x,
                            m1 = y,
                        })
                    .SelectMany(x => x.m1.DefaultIfEmpty(),
                        (x, y) => new
                        {
                            t0 = x.t1,
                            m1 = y,
                        });

                var query2 = from a in query1
                    join b in bankRepo.GetAll().Where(x => !x.IsDeleted) on a.t0.t0.BankCode equals b.BankCode into
                        joinedBank
                    from bank in joinedBank.DefaultIfEmpty()
                    select new TransactionPagingDto
                    {
                        transactionId = a.t0.t0.Id,
                        MerchantId = a.t0.t0.MerchantId,
                        merchantName = a.t0.t0.MerchantName,
                        merchantCode = a.t0.t0.MerchantCode,
                        mainBranchId = a.t0.m0.MerchantCode,
                        amount = Math.Round(a.t0.t0.Amount, 2),
                        paidAmount = a.t0.t0.PaidAmount,
                        transactionNo = a.t0.t0.TransactionNo,
                        InvoiceNo = a.t0.t0.InvoiceNo,
                        transactionDate =
                            CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(a.t0.t0.CreatedTimestamp,
                                "yyyy-MM-dd HH:mm:ss"),

                        paymentChannel =
                            a.t0.t0.PaymentChannel == PaymentChannelType.TrueMoneyCtoBOffline.Name ||
                            a.t0.t0.PaymentChannel == PaymentChannelType.TrueMoneyCtoBOnline.Name
                                ? "truemoney (c scan b)"
                                : a.t0.t0.PaymentChannel,
                        fee = Math.Round(a.t0.t0.Fee, 2),
                        vat = Math.Round(a.t0.t0.FeeVat, 2),

                        netAmount = Math.Round(a.t0.t0.Balance, 2),
                        TransactionStatusId = a.t0.t0.TransactionStatusId,
                        transactionStatusName = TransactionStatus.FromStatusId(a.t0.t0.TransactionStatusId),
                        source = a.t0.t0.SourceName,
                        invoiceRef = a.t0.t0.InvoiceRef,
                        sapCustomerId = a.m1.SapCustomerId,
                        merchantCategoryName = a.m1.MerchantCategoryName,
                        transferDateTime =
                            CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(a.t0.t0.TransferDateTime,
                                "yyyy-MM-dd"),
                        withHoldingTax = a.t0.t0.WithHoldingTax,
                        merchantServiceType = a.t0.t0.MerchantServiceType,
                        internalOrder = a.t0.t0.InternalOrder,
                        description = a.t0.t0.Description,
                        providerName = bank.BankName,
                        paidAt = CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(a.t0.t0.PaidAt,
                            "yyyy-MM-dd HH:mm:ss"),
                        chargeId = a.t0.t0.ChargeId,
                    };


                //get reconcile 
                var reconcileProcessRepo = unitOfWork.GetRepository<ReconcileProcessEntity>();
                var reconcileProcessDetailRepo = unitOfWork.GetRepository<ReconcileProcessDetailsEntity>();
                var settlementReportDetailsRepo = unitOfWork.GetRepository<SettlementReportDetailsEntity>();

                var reconcileData = reconcileProcessRepo.GetAll()
                    .Where(x => !x.IsDeleted && x.ProcessStatus == "Success")
                    .Join(bankRepo.GetAll().Where(x => !x.IsDeleted), a => a.Issuer, b => b.BankCode,
                        (a, b) => new
                        {
                            reconcileProcesses = a,
                            IssuerCode = b.BankCode,
                            IssuerName = b.BankName,
                            a.ReportFileName,
                        }).Join(reconcileProcessDetailRepo.GetAll().Where(x => !x.IsDeleted),
                        a => a.reconcileProcesses.Id, b => b.ReconcileProcessId,
                        (a, b) => new
                        {
                            issuer = a.IssuerName,
                            issuerCode = a.IssuerCode,
                            reconcileProcessId = a.reconcileProcesses.Id,
                            paymentmethod = b.PaymentMethod,
                            estimatedCashInDate =
                                CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(b.EstimatedCashInDate,
                                    "dd/MM/yyyy"),
                            reportFileName = a.ReportFileName,
                            reconcileReportNo = a.reconcileProcesses.ReportNo,
                        }).Join(settlementReportDetailsRepo.GetAll().Where(x => !x.IsDeleted),
                        a => a.reconcileProcessId, b => b.ReconcileProcessId,
                        (a, b) => new
                        {
                            paymentmethod = a.paymentmethod.ToLower(),
                            transactionNo = b.ReferenceOrder,
                            a.issuer,
                            a.issuerCode,
                            a.estimatedCashInDate,
                            bahtAmount = b.BahtAmount.ToString(),
                            bahtCommAmount = b.BahtCommAmount.ToString(),
                            bahtVAT = b.BahtVAT.ToString(),
                            WHT = b.WHT.ToString(),
                            bahtNetAmount = b.BahtNetAmount.ToString(),
                            bankReferenceOrder = b.BankReferenceOrder,
                            a.reportFileName,
                            authDateTime = b.AuthDateTime.HasValue
                                ? CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(b.AuthDateTime.Value,
                                    "yyyy-MM-dd HH:mm:ss")
                                : "",
                            a.reconcileReportNo,
                            BahtNetWHTAmount = b.BahtNetWHTAmount.ToString(),
                            test = b.BahtNetWHTAmount,
                        });
                query2 = query2.GroupJoin(reconcileData,
                        a => new
                        {
                            a.transactionNo,
                            paymentChannel = (a.paymentChannel == "promptpay (flatrate)") ? "promptpay"
                                : a.paymentChannel.ToLower().Contains("alipay") ? "alipay"
                                : a.paymentChannel.ToLower().Contains("wechatpay") ? "wechatpay"
                                : a.paymentChannel.ToLower().Contains("truemoney") ? "truemoney"
                                : a.paymentChannel
                        },
                        b => new { b.transactionNo, paymentChannel = b.paymentmethod },
                        (a, b) => new { query = a, reconcile = b })
                    .SelectMany(x => x.reconcile.DefaultIfEmpty(),
                        (a, b) => new TransactionPagingDto
                        {
                            transactionId = a.query.transactionId,
                            MerchantId = a.query.MerchantId,
                            merchantName = a.query.merchantName,
                            merchantCode = a.query.merchantCode,
                            mainBranchId = a.query.mainBranchId,
                            amount = a.query.amount,
                            paidAmount = a.query.paidAmount,
                            transactionNo = a.query.transactionNo,
                            InvoiceNo = a.query.InvoiceNo,
                            transactionDate = a.query.transactionDate,
                            paymentChannel = a.query.paymentChannel,
                            fee = a.query.fee,
                            vat = a.query.vat,

                            netAmount = a.query.netAmount,
                            TransactionStatusId = a.query.TransactionStatusId,
                            transactionStatusName = a.query.transactionStatusName,
                            source = a.query.source,
                            invoiceRef = a.query.invoiceRef,
                            internalOrder = a.query.internalOrder,
                            sapCustomerId = a.query.sapCustomerId,
                            merchantCategoryName = a.query.merchantCategoryName,
                            transferDateTime = a.query.transferDateTime,
                            withHoldingTax = a.query.withHoldingTax,
                            issuer = b.issuer,
                            issuerCode = b.issuerCode,
                            estimatedCashInDate = b.estimatedCashInDate,
                            bahtAmount = decimal
                                .Round(decimal.Parse(b.bahtAmount ?? "0"), 2, MidpointRounding.AwayFromZero)
                                .ToString("#,##0.00"),
                            bahtCommAmount = decimal
                                .Round(decimal.Parse(b.bahtCommAmount ?? "0"), 2, MidpointRounding.AwayFromZero)
                                .ToString("#,##0.00"),
                            bahtVAT = decimal.Round(decimal.Parse(b.bahtVAT ?? "0"), 2, MidpointRounding.AwayFromZero)
                                .ToString("#,##0.00"),
                            bahtNetAmount = decimal
                                .Round(decimal.Parse(b.bahtNetAmount ?? "0"), 2, MidpointRounding.AwayFromZero)
                                .ToString("#,##0.00"),
                            wt = decimal.Round(decimal.Parse(b.WHT ?? "0"), 2, MidpointRounding.AwayFromZero)
                                .ToString("#,##0.00"),
                            wtNetAmount = decimal.Round(decimal.Parse(b.BahtNetWHTAmount ?? "0"), 2,
                                MidpointRounding.AwayFromZero).ToString("#,##0.00"),
                            description = a.query.description,
                            merchantServiceType = a.query.merchantServiceType,
                            bankReferenceOrder = b.bankReferenceOrder,
                            reportName = b.reportFileName,
                            authDateTime = b.authDateTime,
                            reconcileReportNo = b.reconcileReportNo,
                            providerName = a.query.providerName,
                            paidAt = a.query.paidAt,
                            chargeId = a.query.chargeId,
                        }).Select(x => x).OrderBy(x => x.transactionNo).Take(100000);

                #endregion

                var countData = await transaction.CountAsync();
                _logger.LogInformation($"[INFO] GetTransactionByPaymentAdminExcel countData : {countData}");
                if (countData > appSettings.MaxPageSize)
                {
                    throw new Exception(
                        $"Exceeded maximum data export limit to {Convert.ToInt32(appSettings.MaxPageSize):N0} records.\nFound {countData:N0} records");
                }

                var resultData = await SetReturnDataByRole(query2.ToList(), resourceParameter.RoleId);
                stopwatch.Stop();
                TimeSpan elapsedTime = stopwatch.Elapsed;
                _logger.LogInformation($"[INFO] GetTransactionByPaymentAdminExcel Process Time : {elapsedTime}");
                var headerColumn = await SetReturnColumnByRole(resourceParameter.RoleId);
                _logger.LogInformation($"[INFO] GetTransactionByPaymentAdminExcel getData success");

                if (resultData.Any())
                {
                    stopwatch.Restart();
                    _logger.LogInformation($"[INFO] GetTransactionByPaymentAdminExcel Start generating excel file...");
                    var excelResult = await CreateTransactionExcelFileList(resultData, headerColumn);
                    stopwatch.Stop();
                    elapsedTime = stopwatch.Elapsed;
                    _logger.LogInformation(
                        $"[INFO] GetTransactionByPaymentAdminExcel Generate success total time : {elapsedTime}ms");

                    return excelResult;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }

        public async Task TransactionAutoSendingReportExcel(TransactionAutoSendingReportrequest request)
        {
            try
            {
                _logger.LogInformation(
                    $"[INFO] GetTransactionByPaymentAdminExcel Start : {JsonConvert.SerializeObject(request)}");
                var transationRepo = unitOfWork.GetRepository<TransactionEntity>();
                var bankRepo = unitOfWork.GetRepository<BankEntity>();
                var transaction = transationRepo.GetAll().Where(x => !x.IsDeleted);

                #region | Filter |

                DateTime dateTimeStart;
                DateTime dateTimeEnd;

                if (!string.IsNullOrWhiteSpace(request.StartDate))
                {
                    DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{request.StartDate} 00:00:00", "yyyy-MM-dd HH:mm:ss");
                    dateTimeStart = Convert.ToDateTime(request.StartDate);
                    transaction = transaction.Where(x => x.PaidAt >= startDate);
                }
                else
                {
                    string date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd 00:00:00");
                    DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{date}", "yyyy-MM-dd HH:mm:ss");
                    dateTimeStart = Convert.ToDateTime(date);
                    _logger.LogInformation($"[INFO] datetime start {dateTimeStart}");
                    transaction = transaction.Where(x => x.PaidAt >= startDate);
                }

                if (!string.IsNullOrWhiteSpace(request.EndDate))
                {
                    DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{request.EndDate} 23:59:59.997", "yyyy-MM-dd HH:mm:ss.fff");
                    dateTimeEnd = Convert.ToDateTime(request.EndDate);
                    transaction = transaction.Where(x => x.PaidAt <= endDate);
                }
                else
                {
                    string date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd 23:59:59");
                    DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{date}", "yyyy-MM-dd HH:mm:ss");
                    dateTimeEnd = Convert.ToDateTime(date);
                    transaction = transaction.Where(x => x.PaidAt <= endDate);
                }

                #endregion

                #region | Query |

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                var transactionStatus = new List<int> { 2, 5 };
                transaction = transaction.Where(x =>
                    transactionStatus.Contains(x.TransactionStatusId) && x.MerchantServiceType == 1);
                var countStatus2 = transaction.Count(c => c.TransactionStatusId == 2);
                var countStatus5 = transaction.Count(c => c.TransactionStatusId == 5);


                var joinMerchantQuery = transaction.Where(n => !n.IsDeleted)
                    .GroupJoin(_context.CustomMerchantEntity,
                        x => x.MainBranchId.HasValue ? x.MainBranchId : x.MerchantId,
                        y => y.Id,
                        (x, y) => new
                        {
                            t0 = x,
                            m0 = y,
                        })
                    .SelectMany(x => x.m0.DefaultIfEmpty(),
                        (x, y) => new
                        {
                            x.t0,
                            m0 = y,
                        })
                    .GroupJoin(_context.CustomMerchantEntity,
                        x => x.t0.MerchantId,
                        y => y.Id,
                        (x, y) => new
                        {
                            t1 = x,
                            m1 = y,
                        })
                    .SelectMany(x => x.m1.DefaultIfEmpty(),
                        (x, y) => new
                        {
                            t0 = x.t1,
                            m1 = y,
                        });

                var joinBankQuery = from a in joinMerchantQuery
                    join b in bankRepo.GetAll().Where(x => !x.IsDeleted) on a.t0.t0.BankCode equals b.BankCode into
                        joinedBank
                    from bank in joinedBank.DefaultIfEmpty()
                    select new TransactionPagingDto
                    {
                        transactionId = a.t0.t0.Id,
                        MerchantId = a.t0.t0.MerchantId,
                        merchantName = a.t0.t0.MerchantName,
                        merchantCode = a.t0.t0.MerchantCode,
                        mainBranchId = a.t0.m0.MerchantCode,
                        amount = Math.Round(a.t0.t0.Amount, 2),
                        paidAmount = a.t0.t0.PaidAmount,
                        transactionNo = a.t0.t0.TransactionNo,
                        InvoiceNo = a.t0.t0.InvoiceNo,
                        transactionDate =
                            CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(a.t0.t0.CreatedTimestamp,
                                "yyyy-MM-dd HH:mm:ss"),

                        paymentChannel =
                            a.t0.t0.PaymentChannel == PaymentChannelType.TrueMoneyCtoBOffline.Name ||
                            a.t0.t0.PaymentChannel == PaymentChannelType.TrueMoneyCtoBOnline.Name
                                ? "truemoney (c scan b)"
                                : a.t0.t0.PaymentChannel,
                        fee = Math.Round(a.t0.t0.Fee, 2),
                        vat = Math.Round(a.t0.t0.FeeVat, 2),

                        netAmount = Math.Round(a.t0.t0.Balance, 2),
                        TransactionStatusId = a.t0.t0.TransactionStatusId,
                        transactionStatusName = TransactionStatus.FromStatusId(a.t0.t0.TransactionStatusId),
                        source = a.t0.t0.SourceName,
                        invoiceRef = a.t0.t0.InvoiceRef,
                        sapCustomerId = a.m1.SapCustomerId,
                        merchantCategoryName = a.m1.MerchantCategoryName,
                        transferDateTime =
                            CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(a.t0.t0.TransferDateTime,
                                "yyyy-MM-dd"),
                        withHoldingTax = a.t0.t0.WithHoldingTax,
                        merchantServiceType = a.t0.t0.MerchantServiceType,
                        internalOrder = a.t0.t0.InternalOrder,
                        description = a.t0.t0.Description,
                        providerName = bank.BankName,
                        paidAt = CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(a.t0.t0.PaidAt,
                            "yyyy-MM-dd HH:mm:ss"),
                    };


                //get reconcile 
                var reconcileProcessRepo = unitOfWork.GetRepository<ReconcileProcessEntity>();
                var reconcileProcessDetailRepo = unitOfWork.GetRepository<ReconcileProcessDetailsEntity>();
                var settlementReportDetailsRepo = unitOfWork.GetRepository<SettlementReportDetailsEntity>();

                var reconcileData = reconcileProcessRepo.GetAll()
                    .Where(x => !x.IsDeleted && x.ProcessStatus == "Success")
                    .Join(bankRepo.GetAll().Where(x => !x.IsDeleted), a => a.Issuer, b => b.BankCode,
                        (a, b) => new
                        {
                            reconcileProcesses = a,
                            IssuerCode = b.BankCode,
                            IssuerName = b.BankName,
                            a.ReportFileName,
                        }).Join(reconcileProcessDetailRepo.GetAll().Where(x => !x.IsDeleted),
                        a => a.reconcileProcesses.Id, b => b.ReconcileProcessId,
                        (a, b) => new
                        {
                            issuer = a.IssuerName,
                            issuerCode = a.IssuerCode,
                            reconcileProcessId = a.reconcileProcesses.Id,
                            paymentmethod = b.PaymentMethod,
                            estimatedCashInDate =
                                CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(b.EstimatedCashInDate,
                                    "dd/MM/yyyy"),
                            reportFileName = a.ReportFileName,
                            reconcileReportNo = a.reconcileProcesses.ReportNo,
                        }).Join(settlementReportDetailsRepo.GetAll().Where(x => !x.IsDeleted),
                        a => a.reconcileProcessId, b => b.ReconcileProcessId,
                        (a, b) => new
                        {
                            paymentmethod = a.paymentmethod.ToLower(),
                            transactionNo = b.ReferenceOrder,
                            a.issuer,
                            a.issuerCode,
                            a.estimatedCashInDate,
                            bahtAmount = b.BahtAmount.ToString(),
                            bahtCommAmount = b.BahtCommAmount.ToString(),
                            bahtVAT = b.BahtVAT.ToString(),
                            WHT = b.WHT.ToString(),
                            bahtNetAmount = b.BahtNetAmount.ToString(),
                            bankReferenceOrder = b.BankReferenceOrder,
                            a.reportFileName,
                            authDateTime = b.AuthDateTime.HasValue
                                ? CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(b.AuthDateTime.Value,
                                    "yyyy-MM-dd HH:mm:ss")
                                : "",
                            a.reconcileReportNo,
                            BahtNetWHTAmount = b.BahtNetWHTAmount.ToString(),
                            test = b.BahtNetWHTAmount,
                        });

                joinBankQuery = joinBankQuery.GroupJoin(reconcileData,
                        a => new
                        {
                            a.transactionNo,
                            paymentChannel = (a.paymentChannel == "promptpay (flatrate)") ? "promptpay"
                                : a.paymentChannel.ToLower().Contains("alipay") ? "alipay"
                                : a.paymentChannel.ToLower().Contains("wechatpay") ? "wechatpay"
                                : a.paymentChannel.ToLower().Contains("truemoney") ? "truemoney"
                                : a.paymentChannel
                        },
                        b => new { b.transactionNo, paymentChannel = b.paymentmethod },
                        (a, b) => new { query = a, reconcile = b })
                    .SelectMany(x => x.reconcile.DefaultIfEmpty(),
                        (a, b) => new TransactionPagingDto
                        {
                            transactionId = a.query.transactionId,
                            MerchantId = a.query.MerchantId,
                            merchantName = a.query.merchantName,
                            merchantCode = a.query.merchantCode,
                            mainBranchId = a.query.mainBranchId,
                            amount = a.query.amount,
                            paidAmount = a.query.paidAmount,
                            transactionNo = a.query.transactionNo,
                            InvoiceNo = a.query.InvoiceNo,
                            transactionDate = a.query.transactionDate,
                            paymentChannel = a.query.paymentChannel,
                            fee = a.query.fee,
                            vat = a.query.vat,

                            netAmount = a.query.netAmount,
                            TransactionStatusId = a.query.TransactionStatusId,
                            transactionStatusName = a.query.transactionStatusName,
                            source = a.query.source,
                            invoiceRef = a.query.invoiceRef,
                            internalOrder = a.query.internalOrder,
                            sapCustomerId = a.query.sapCustomerId,
                            merchantCategoryName = a.query.merchantCategoryName,
                            transferDateTime = a.query.transferDateTime,
                            withHoldingTax = a.query.withHoldingTax,
                            issuer = b.issuer,
                            issuerCode = b.issuerCode,
                            estimatedCashInDate = b.estimatedCashInDate,
                            bahtAmount = decimal
                                .Round(decimal.Parse(b.bahtAmount ?? "0"), 2, MidpointRounding.AwayFromZero)
                                .ToString("#,##0.00"),
                            bahtCommAmount = decimal
                                .Round(decimal.Parse(b.bahtCommAmount ?? "0"), 2, MidpointRounding.AwayFromZero)
                                .ToString("#,##0.00"),
                            bahtVAT = decimal.Round(decimal.Parse(b.bahtVAT ?? "0"), 2, MidpointRounding.AwayFromZero)
                                .ToString("#,##0.00"),
                            bahtNetAmount = decimal
                                .Round(decimal.Parse(b.bahtNetAmount ?? "0"), 2, MidpointRounding.AwayFromZero)
                                .ToString("#,##0.00"),
                            wt = decimal.Round(decimal.Parse(b.WHT ?? "0"), 2, MidpointRounding.AwayFromZero)
                                .ToString("#,##0.00"),
                            wtNetAmount = decimal.Round(decimal.Parse(b.BahtNetWHTAmount ?? "0"), 2,
                                MidpointRounding.AwayFromZero).ToString("#,##0.00"),
                            description = a.query.description,
                            merchantServiceType = a.query.merchantServiceType,
                            bankReferenceOrder = b.bankReferenceOrder,
                            reportName = b.reportFileName,
                            authDateTime = b.authDateTime,
                            reconcileReportNo = b.reconcileReportNo,
                            providerName = a.query.providerName,
                            paidAt = a.query.paidAt
                        }).Select(x => x).OrderBy(x => x.transactionNo).Take(100000);

                #endregion

                var countData = await transaction.CountAsync();
                _logger.LogInformation($"[INFO] GetTransactionByPaymentAdminExcel countData : {countData}");
                if (countData > appSettings.MaxPageSize)
                {
                    throw new Exception(
                        $"Exceeded maximum data export limit to {Convert.ToInt32(appSettings.MaxPageSize):N0} records.\nFound {countData:N0} records");
                }

                var roles = appSettings.Role;

                var roleAccounting = roles.Where(x => x.RoleName.ToLower() == "accounting").FirstOrDefault();
                var roleId = Guid.Parse(roleAccounting.RoleId);
                _logger.LogInformation($"[INFO] Accounting RoleId: {roleId}");
                var resultData = await SetReturnDataByRole(joinBankQuery.ToList(), roleId);
                if (resultData.Any())
                {
                    decimal? sumPaidAmount = resultData.Sum(o => o.paidAmount);
                    stopwatch.Stop();
                    TimeSpan elapsedTime = stopwatch.Elapsed;
                    _logger.LogInformation($"[INFO] GetTransactionByPaymentAdminExcel Process Time : {elapsedTime}");
                    var headerColumn = await SetReturnColumnByRole(roleId);
                    _logger.LogInformation($"[INFO] GetTransactionByPaymentAdminExcel getData success");

                    stopwatch.Restart();
                    _logger.LogInformation($"[INFO] GetTransactionByPaymentAdminExcel Start generating excel file...");
                    var excelResult = await CreateTransactionExcelFileList(resultData, headerColumn);
                    elapsedTime = stopwatch.Elapsed;
                    _logger.LogInformation(
                        $"[INFO] GetTransactionByPaymentAdminExcel Generate success total time : {elapsedTime}ms");
                    string filename;
                    string subject;
                    string body;
                    string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                    env = env.ToLower() != "production" ? $"[{env.ToUpper()}]" : string.Empty;
                    
                    if (!string.IsNullOrEmpty(request.EndDate))
                    {
                        filename = $"TransactionReport_{dateTimeStart:yyyyMMdd}_{dateTimeEnd:yyyyMMdd}.xlsx"; 
                        subject = $"{env}[Report] Transaction {dateTimeStart:yyyyMMdd} - {dateTimeEnd:yyyyMMdd}";
                        body = $"{dateTimeStart:yyyy-MM-dd} - {dateTimeEnd:yyyy-MM-dd}";
                    }
                    else
                    {
                        filename = $"TransactionReport_{dateTimeStart:yyyyMMdd}.xlsx";
                        subject = $"{env}[Report] Transaction {dateTimeStart:yyyyMMdd}";
                        body = $"{dateTimeStart:yyyy-MM-dd}";
                    }
                    _logger.LogInformation($"[INFO] GetTransactionByPaymentAdminExcel Generate filename: {filename}");
                    string paidAmountStr = sumPaidAmount?.ToString("#,##0.00");
                    string mailboy = $@"
                   <h3><b>Transaction Report :</b> {body}</h3><br><br>
                   &nbsp;&nbsp;&nbsp;&nbsp;<b>จำนวน Transaction ทั้งหมด : </b>{countData:#,##0} รายการ<br>
                   &nbsp;&nbsp;&nbsp;&nbsp;<b>จำนวนรายการที่ชำระเต็มจำนนวน : </b>{countStatus2:#,##0} รายการ<br>
                   &nbsp;&nbsp;&nbsp;&nbsp;<b>จำนวนรายการที่ชำระไม่เต็มจำนวน : </b>{countStatus5:#,##0} รายการ<br>
                   &nbsp;&nbsp;&nbsp;&nbsp;<b>จำนวนเงินรวมทั้งหมด : </b>{paidAmountStr} บาท<br>";
                   
                    
                    _logger.LogInformation($"[INFO] GetTransactionByPaymentAdminExcel Generate subject: {subject}");
                    await _emailAttachment.SendEmailAsync(roleAccounting.Emails, mailboy, excelResult, filename,subject);
                }
                else
                {
                    _logger.LogDebug($"[DEBUG] Result data is null");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<byte[]> GetTransactionByPaymentMerchantExport(Guid merchantId,
            TransactionPagingParameters resourceParameter)
        {
            try
            {
                // list merchantId of branch 
                var merchantRepo = unitOfWork.GetRepository<MerchantEntity>();
                var merchantsQuery = merchantRepo.GetAll().Where(x => x.MainBranchId == merchantId && !x.IsDeleted)
                    .Select(x => x.Id);

                var merchants = await merchantsQuery.ToListAsync();

                // add default merchantId to merchants
                merchants.Add(merchantId);

                var transationRepo = unitOfWork.GetRepository<TransactionEntity>();
                var transaction = transationRepo.GetAll().Where(x =>
                    merchants.Contains(x.MerchantId) && !x.IsDeleted
                );

                if (!string.IsNullOrWhiteSpace(resourceParameter.StartDate))
                {
                    DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{resourceParameter.StartDate} 00:00:00", "yyyy-MM-dd HH:mm:ss");

                    transaction = transaction.Where(x => x.CreatedTimestamp >= startDate);
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.EndDate))
                {
                    DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{resourceParameter.EndDate} 23:59:59.997", "yyyy-MM-dd HH:mm:ss.fff");

                    transaction = transaction.Where(x => x.CreatedTimestamp <= endDate);
                }

                if (resourceParameter.PaymentChannels != null && resourceParameter.PaymentChannels.Any())
                {
                    var PaymentChannels =
                        ConvertStringArrayOrStringSplitToListString(resourceParameter.PaymentChannels);

                    if (PaymentChannels.Count > 0)
                    {
                        if (PaymentChannels.Contains(PaymentChannelType.PromptPay.Name))
                        {
                            PaymentChannels.Add(PaymentChannelType.PromptPayFlatRate.Name);
                        }

                        if (PaymentChannels.Contains(PaymentChannelType.TrueMoneyCtoBOffline.Name) ||
                            PaymentChannels.Contains(PaymentChannelType.TrueMoneyCtoB.Name))
                        {
                            PaymentChannels.Remove(PaymentChannelType.TrueMoneyCtoBOffline.Name);
                            PaymentChannels.Remove(PaymentChannelType.TrueMoneyCtoB.Name);
                            PaymentChannels.Add(PaymentChannelType.TrueMoneyCtoBOnline.Name);
                            PaymentChannels.Add(PaymentChannelType.TrueMoneyCtoBOffline.Name);
                        }

                        transaction = transaction.Where(x => PaymentChannels.Contains(x.PaymentChannel.ToLower()));
                    }
                }

                if (resourceParameter.Sources != null && resourceParameter.Sources.Any())
                {
                    var Sources = ConvertStringArrayOrStringSplitToListString(resourceParameter.Sources);

                    if (Sources.Count > 0)
                    {
                        transaction = transaction.Where(x => Sources.Contains(x.SourceName.ToLower()));
                    }
                }

                if (resourceParameter.TransactionStatus != null && resourceParameter.TransactionStatus.Any())
                {
                    var TransactionStatus =
                        ConvertStringArrayOrStringSplitToListString(resourceParameter.TransactionStatus);

                    if (TransactionStatus.Count > 0)
                    {
                        transaction = transaction.Where(x =>
                            TransactionStatus.Contains(x.TransactionStatusId.ToString()));
                    }
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.Keyword))
                {
                    transaction = transaction.Where(x => x.MerchantName.Contains(resourceParameter.Keyword) ||
                                                         x.TransactionNo.Contains(resourceParameter.Keyword));
                }

                // order by transaction
                if (!string.IsNullOrWhiteSpace(resourceParameter.OrderBy.ToString()) &&
                    !string.IsNullOrWhiteSpace(resourceParameter.Order.ToString()))
                {
                    transaction = TransactionEntitySortable.CustomSort(transaction, resourceParameter.OrderBy,
                        resourceParameter.Order);
                }

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                var result = transaction.Select(x => new TransactionPagingDto
                {
                    transactionId = x.Id,
                    MerchantId = x.MerchantId,
                    merchantName = x.MerchantName,
                    merchantCode = x.MerchantCode,
                    amount = Math.Round(x.Amount, 2),
                    paidAmount = x.PaidAmount,
                    transactionNo = x.TransactionNo,
                    InvoiceNo = x.InvoiceNo,
                    transactionDate =
                        CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(x.CreatedTimestamp,
                            "yyyy-MM-dd HH:mm:ss"),
                    paymentChannel =
                        x.PaymentChannel == PaymentChannelType.TrueMoneyCtoBOffline.Name ||
                        x.PaymentChannel == PaymentChannelType.TrueMoneyCtoBOnline.Name
                            ? "truemoney (c scan b)"
                            : x.PaymentChannel,
                    fee = Math.Round(x.Fee, 2),
                    vat = Math.Round(x.FeeVat, 2),

                    netAmount = Math.Round(x.Balance, 2),
                    TransactionStatusId = x.TransactionStatusId,
                    transactionStatusName = TransactionStatus.FromStatusId(x.TransactionStatusId),
                    source = x.SourceName,
                    invoiceRef = x.InvoiceRef,
                    transferDateTime =
                        CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(x.TransferDateTime,
                            "yyyy-MM-dd HH:mm:ss"),
                    withHoldingTax = x.WithHoldingTax,
                    description = x.Description,
                    paidAt = CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(x.PaidAt,
                        "yyyy-MM-dd HH:mm:ss"),
                });
                var countData = await result.CountAsync();
                _logger.LogInformation($"[INFO] GetTransactionByPaymentMerchantExport countData : {countData}");

                if (countData > appSettings.MaxPageSize)
                {
                    throw new Exception(
                        $"Exceeded maximum data export limit to {Convert.ToInt32(appSettings.MaxPageSize):N0} records.\nFound {countData:N0} records");
                }

                if (countData == 0)
                {
                    throw new Exception("Data not found");
                }

                var resultData = result.Skip((resourceParameter.Page - 1) * resourceParameter.PageSize)
                    .Take(resourceParameter.PageSize).ToList();


                _logger.LogInformation($"[INFO] GetTransactionByPaymentMerchantExport Start generating excel file...");
                var excelResult = await CreateExcelMerchantTransaction(resultData);
                stopwatch.Stop();
                TimeSpan elapsedTime = stopwatch.Elapsed;
                _logger.LogInformation(
                    $"[INFO] GetTransactionByPaymentMerchantExport Generate success total time : {elapsedTime}ms");

                return excelResult;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private List<string> ConvertStringArrayOrStringSplitToListString(List<string> input)
        {
            var ret = new List<string>();

            foreach (var inputItem in input)
            {
                if (string.IsNullOrWhiteSpace(inputItem))
                {
                    continue;
                }

                ret.AddRange(inputItem.Trim().ToLower().Split(',').ToList());
            }

            return ret;
        }

        public async Task<PagedList<GetTransactionAdjustmentDto>> GetTransactionAdjustment(
            GetTransactionAdjustmentRequest request)
        {
            try
            {
                var transationRepo = unitOfWork.GetRepository<TransactionEntity>();
                var bankRepo = unitOfWork.GetRepository<BankEntity>();
                var transaction = transationRepo.GetAll().Where(x => !x.IsDeleted);
                var merchantRepo = unitOfWork.GetRepository<MerchantEntity>();
                var merchantActive = merchantRepo.GetAll(false).Where(a => !a.IsDeleted);
                var reconcileProcessRepo = unitOfWork.GetRepository<ReconcileProcessEntity>();
                var reconcileProcessActive = reconcileProcessRepo.GetAll(false).Where(a => !a.IsDeleted);
                var reconsileProcessDetailRepo = unitOfWork.GetRepository<ReconcileProcessDetailsEntity>();
                var reconsileProcessDetailActive = reconsileProcessDetailRepo.GetAll(false).Where(a => !a.IsDeleted);
                var settlementReportDetailRepo = unitOfWork.GetRepository<SettlementReportDetailsEntity>();
                var settlementReportDetailActive = settlementReportDetailRepo.GetAll(false).Where(a => !a.IsDeleted);

                #region | filter |

                if (!string.IsNullOrWhiteSpace(request.StartDate))
                {
                    DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{request.StartDate} 00:00:00", "yyyy-MM-dd HH:mm:ss");

                    transaction = transaction.Where(x => x.CreatedTimestamp >= startDate);
                }

                if (!string.IsNullOrWhiteSpace(request.EndDate))
                {
                    DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{request.EndDate} 23:59:59.997", "yyyy-MM-dd HH:mm:ss.fff");

                    transaction = transaction.Where(x => x.CreatedTimestamp <= endDate);
                }

                if (request.PaymentChannels != null && request.PaymentChannels.Any())
                {
                    var PaymentChannels =
                        ConvertStringArrayOrStringSplitToListString(request.PaymentChannels);

                    if (PaymentChannels.Count > 0)
                    {
                        if (PaymentChannels.Contains(PaymentChannelType.PromptPay.Name))
                        {
                            PaymentChannels.Add(PaymentChannelType.PromptPayFlatRate.Name);
                        }

                        if (PaymentChannels.Contains(PaymentChannelType.TrueMoneyCtoB.Name))
                        {
                            PaymentChannels.Add(PaymentChannelType.TrueMoneyCtoBOffline.Name);
                            PaymentChannels.Add(PaymentChannelType.TrueMoneyCtoBOnline.Name);
                        }

                        transaction = transaction.Where(x => PaymentChannels.Contains(x.PaymentChannel.ToLower()));
                    }
                }

                if (request.TransactionStatus != null && request.TransactionStatus.Any())
                {
                    var TransactionStatus =
                        ConvertStringArrayOrStringSplitToListString(request.TransactionStatus);

                    if (TransactionStatus.Count > 0)
                    {
                        transaction = transaction.Where(x =>
                            TransactionStatus.Contains(x.TransactionStatusId.ToString()));
                    }
                }

                if (!string.IsNullOrEmpty(request.ServiceType))
                {
                    var serviceType = request.ServiceType == "PF" ? 1 : 2;
                    transaction = transaction.Where(x => x.MerchantServiceType == serviceType);
                }

                if (!string.IsNullOrEmpty(request.PaymentType))
                {
                    transaction = transaction.Where(x => x.SourceName == request.PaymentType);
                }

                #endregion

                string formatDatetime = "yyyy-MM-dd HH:mm:ss";
                var totalData = await transaction.CountAsync();
                transaction = transaction.OrderBy(x => x.CreatedTimestamp).ThenBy(x => x.TransactionNo);

                transaction = transaction.Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize);

                var transactionjoin = from tran in transaction
                    join merchant in merchantActive on tran.MerchantId equals merchant.Id into merchantInfo
                    from merchant in merchantInfo.DefaultIfEmpty()
                    join bank in bankRepo.GetAll(false).Where(a => !a.IsDeleted) on tran.BankCode equals bank.BankCode
                        into bankInfo
                    from bank in bankInfo.DefaultIfEmpty()
                    join reconcileProcess in reconcileProcessActive on bank.BankCode equals reconcileProcess.Issuer into
                        reconcileProcessInfo
                    from reconcileProcess in reconcileProcessInfo.DefaultIfEmpty()
                    join reconsileProcessDetail in reconsileProcessDetailActive on reconcileProcess.Id equals
                        reconsileProcessDetail.ReconcileProcessId into reconsileProcessDetailInfo
                    from reconsileProcessDetail in reconsileProcessDetailInfo.DefaultIfEmpty()
                    join settlementReportDetail in settlementReportDetailActive on tran.TransactionNo equals
                        settlementReportDetail.ReferenceOrder into settlementReportDetailInfo
                    from settlementReportDetail in settlementReportDetailInfo.DefaultIfEmpty()
                    select new GetTransactionAdjustmentDto
                    {
                        AdjustDateTime = null, // wait for create new table
                        Amount = tran.Amount,
                        BusinessType = merchant.MerchantCategoryName,
                        AuthorizeDateTime = tran.PaidAt.HasValue
                            ? tran.PaidAt.Value.ToString(formatDatetime)
                            : string.Empty,
                        CashReceiveDate = reconsileProcessDetail.EstimatedCashInDate.HasValue
                            ? reconsileProcessDetail.EstimatedCashInDate.Value.ToString(formatDatetime)
                            : string.Empty,
                        CancelDatetime = tran.CancelAt.HasValue
                            ? tran.CancelAt.Value.ToString(formatDatetime)
                            : string.Empty,
                        CreateDateTime = tran.CreatedTimestamp.HasValue
                            ? tran.CreatedTimestamp.Value.ToString(formatDatetime)
                            : string.Empty,
                        Description = tran.Description,
                        FeeAmount = tran.Fee,
                        MdrTotal = tran.Mdr,
                        MerchantIdParent = merchant.MainBranchName,
                        MerchantName = merchant.MerchantName,
                        MerchantOrderNo = tran.InvoiceNo,
                        NetAmount = Math.Round(tran.Balance, 2),
                        NetAmountWHT =
                            decimal.Round(decimal.Parse(settlementReportDetail.BahtNetWHTAmount.ToString() ?? "0"), 2,
                                MidpointRounding.AwayFromZero),
                        PaidAmount = tran.PaidAmount,
                        PayAmount = tran.Amount,
                        PaybackDate = tran.TransferDateTime.HasValue
                            ? tran.TransferDateTime.Value.ToString(formatDatetime)
                            : string.Empty,
                        PaymentChannel = tran.PaymentChannel,
                        PaymentStatus = TransactionStatus.FromStatusId(tran.TransactionStatusId),
                        PaymentTerm = merchant.PaymentTerm,
                        PaymentType = tran.SourceName,
                        ProviderName = bank.BankName,
                        ReconcileReportNo = reconcileProcess.ReportNo,
                        SapCustomerId = merchant.SapCustomerId,
                        SapInternalOrder = tran.InternalOrder,
                        SapInvoiceRef = tran.InvoiceRef,
                        ServiceType = tran.MerchantServiceType == 1 ? "PF" : "PPAS",
                        SourceOfFund = tran.PaymentChannel,
                        TaxAmount = decimal.Round(decimal.Parse(tran.FeeVat.ToString() ?? "0"), 2,
                            MidpointRounding.AwayFromZero),
                        TransactionId = tran.Id,
                        TransactionNo = tran.TransactionNo,
                        TransferInfrom = reconcileProcess.Issuer,
                        WHT = tran.WithHoldingTax,
                    };

                var test = await transactionjoin.ToListAsync();

                return await PagedList<GetTransactionAdjustmentDto>.ToPagedList(transactionjoin, request.Page,
                    request.PageSize, totalData);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        #region | Private Method |

        private async Task<List<TransactionPagingDto>> SetReturnDataByRole(List<TransactionPagingDto> obj, Guid roleId)
        {
            if (obj.Any())
            {
                var adminMenuSubLevelRepo = unitOfWork.GetRepository<AdminMenuSubLevelsEntity>();
                var adminRoleSubLevelRepo = unitOfWork.GetRepository<AdminRoleSubLevelEntity>();

                var adminMenuSubLevel = await adminMenuSubLevelRepo.GetAll().Where(a => a.IsActive).ToListAsync();
                var adminRoleSubLevel = await adminRoleSubLevelRepo.GetAll()
                    .Where(a => a.IsActive && a.RoleId == roleId).ToListAsync();

                var menuSub = new List<AdminMenuSubLevelsEntity>();


                if (adminRoleSubLevel.Any())
                {
                    var listSubLevelId = adminRoleSubLevel.Where(a => a.Action.ToLower().Equals("view".ToLower()))
                        .Select(a => a.AdminSubLevelId).ToList();
                    menuSub = adminMenuSubLevel.Where(a => !listSubLevelId.Contains(a.Id)).ToList();
                }
                else if (adminMenuSubLevel.Any())
                {
                    menuSub = adminMenuSubLevel;
                }
                else
                {
                    throw new Exception("Menu Sub Level Not Found !!!");
                }

                foreach (var menu in menuSub)
                {
                    object objLock = new object();
                    Parallel.ForEach(obj, item =>
                    {
                        lock (objLock)
                        {
                            switch (menu.Id)
                            {
                                case Guid io when io == ConfigAdminMenuSubLevelId.SapInternalOrder:
                                    item.internalOrder = null;
                                    break;
                                case Guid fee when fee == ConfigAdminMenuSubLevelId.FeeAmount:
                                    item.fee = null;
                                    break;
                                case Guid feeVatAmount when feeVatAmount == ConfigAdminMenuSubLevelId.TaxAmount:
                                    item.vat = null;
                                    break;
                                case Guid wt when wt == ConfigAdminMenuSubLevelId.ComVAT:
                                    item.bahtVAT = null;
                                    break;
                                case Guid netAmount when netAmount == ConfigAdminMenuSubLevelId.NetAmount:
                                    item.netAmount = null;
                                    break;
                                case Guid paybackDate when paybackDate == ConfigAdminMenuSubLevelId.PaybackDate:
                                    item.transferDateTime = null;
                                    break;
                                case Guid transferFrom when transferFrom == ConfigAdminMenuSubLevelId.TransferInFrom:
                                    item.issuer = null;
                                    break;
                                case Guid estimatedCashInDate
                                    when estimatedCashInDate == ConfigAdminMenuSubLevelId.CashReceiveDate:
                                    item.estimatedCashInDate = null;
                                    break;
                                case Guid reconcileReportNo
                                    when reconcileReportNo == ConfigAdminMenuSubLevelId.ReconcileReportNo:
                                    item.reconcileReportNo = null;
                                    break;
                                case Guid totalAmount when totalAmount == ConfigAdminMenuSubLevelId.Amount:
                                    item.bahtAmount = null;
                                    break;
                                case Guid commissionAmount when commissionAmount == ConfigAdminMenuSubLevelId.ComAmount:
                                    item.bahtCommAmount = null;
                                    break;
                                case Guid comsVATAmount when comsVATAmount == ConfigAdminMenuSubLevelId.ComVAT:
                                    item.bahtVAT = null;
                                    break;
                                case Guid netReceiveFromBank
                                    when netReceiveFromBank == ConfigAdminMenuSubLevelId.NetReceiveFromBank:
                                    item.bahtNetAmount = null;
                                    break;
                                case Guid wht when wht == ConfigAdminMenuSubLevelId.WHT:
                                    item.withHoldingTax = null;
                                    break;
                                case Guid netAmountAfterWHT
                                    when netAmountAfterWHT == ConfigAdminMenuSubLevelId.NetAmountWHT:
                                    item.wtNetAmount = null;
                                    break;
                                default: break;
                            }
                        }
                    });
                }
            }

            return obj;
        }

        private async Task<List<HeaderCoulum>> SetReturnColumnByRole(Guid roleId)
        {
            var result = new List<HeaderCoulum>();
            var adminMenuSubLevelRepo = unitOfWork.GetRepository<AdminMenuSubLevelsEntity>();
            var adminRoleSubLevelRepo = unitOfWork.GetRepository<AdminRoleSubLevelEntity>();

            var adminMenuSubLevel = await adminMenuSubLevelRepo.GetAll().Where(a => a.IsActive).ToListAsync();
            var adminRoleSubLevel = await adminRoleSubLevelRepo.GetAll().Where(a => a.IsActive && a.RoleId == roleId)
                .ToListAsync();

            if (adminRoleSubLevel.Any())
            {
                var listSubLevelId = adminRoleSubLevel.Where(a => a.Action.ToLower().Equals("view".ToLower()))
                    .Select(a => a.AdminSubLevelId).ToList();
                var joinData = from id in listSubLevelId
                    join detail in adminMenuSubLevel on id equals detail.Id
                    select new
                    {
                        detail.Id,
                        detail.SubLevelName
                    };

                result = joinData.Select(a => new HeaderCoulum { Id = a.Id, MenuSubName = a.SubLevelName })
                    .OrderBy(a => a.Id).ToList();
            }

            return result;
        }

        private async Task<byte[]> CreateTransactionExcelFileList(List<TransactionPagingDto> resultData,
            List<HeaderCoulum> listColumns)
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                _logger.LogInformation($"CreateTransactionExcelFileList Start");

                // Set the license context
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    // Create a worksheet
                    var worksheet = package.Workbook.Worksheets.Add("Transactions");


                    // Set the headers

                    #region | Set Header |

                    int indexRowHeader = 1;
                    int indexColumnHeader = 18;
                    worksheet.Cells[indexRowHeader, 1].Value = "Service Type";
                    worksheet.Cells[indexRowHeader, 2].Value = "Create Date Time";
                    worksheet.Cells[indexRowHeader, 3].Value = "Authorize Date Time";
                    worksheet.Cells[indexRowHeader, 4].Value = "Merchant ID - Parent";
                    worksheet.Cells[indexRowHeader, 5].Value = "Merchant ID";
                    worksheet.Cells[indexRowHeader, 6].Value = "Merchant Name";
                    worksheet.Cells[indexRowHeader, 7].Value = "SAP Customer ID";
                    worksheet.Cells[indexRowHeader, 8].Value = "Business Type";
                    worksheet.Cells[indexRowHeader, 9].Value = "SAP Invoice Ref";
                    worksheet.Cells[indexRowHeader, 10].Value = "Transaction No";
                    worksheet.Cells[indexRowHeader, 11].Value = "Provider Transaction Id";
                    worksheet.Cells[indexRowHeader, 12].Value = "Description";
                    worksheet.Cells[indexRowHeader, 13].Value = "SAP Internal Order";
                    worksheet.Cells[indexRowHeader, 14].Value = "Merchant Order No";
                    worksheet.Cells[indexRowHeader, 15].Value = "Source of Fund";
                    worksheet.Cells[indexRowHeader, 16].Value = "Pay Amount";
                    worksheet.Cells[indexRowHeader, 17].Value = "Paid Amount";

                    if (listColumns.Any(a => a.MenuSubName.ToLower() == "fee amount"))
                    {
                        worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Fee Amount";
                        indexColumnHeader++;
                    }

                    if (listColumns.Any(a => a.MenuSubName.ToLower() == "tax amount"))
                    {
                        worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Tax Amount";
                        indexColumnHeader++;
                    }

                    if (listColumns.Any(a => a.MenuSubName.ToLower() == "wht"))
                    {
                        worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "WHT";
                        indexColumnHeader++;
                    }

                    if (listColumns.Any(a => a.MenuSubName.ToLower() == "net amount"))
                    {
                        worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Net Amount";
                        indexColumnHeader++;
                    }

                    worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Payment Type";
                    indexColumnHeader++;
                    worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Payment Status";
                    indexColumnHeader++;
                    worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Provider Name";
                    indexColumnHeader++;
                    if (listColumns.Any(a => a.MenuSubName.ToLower() == "payback date"))
                    {
                        worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Payback Date";
                        indexColumnHeader++;
                    }

                    if (listColumns.Any(a => a.MenuSubName.ToLower() == "transfer in from"))
                    {
                        worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Transfer in from";
                        indexColumnHeader++;
                    }

                    if (listColumns.Any(a => a.MenuSubName.ToLower() == "cash receive date"))
                    {
                        worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Cash Receive Date";
                        indexColumnHeader++;
                    }

                    if (listColumns.Any(a => a.MenuSubName.ToLower() == "reconcile report no"))
                    {
                        worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Reconcile Report No";
                        indexColumnHeader++;
                    }

                    if (listColumns.Any(a => a.MenuSubName.ToLower() == "amount"))
                    {
                        worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Amount";
                        indexColumnHeader++;
                    }

                    if (listColumns.Any(a => a.MenuSubName.ToLower() == "com amount"))
                    {
                        worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Com Amount";
                        indexColumnHeader++;
                    }

                    if (listColumns.Any(a => a.MenuSubName.ToLower() == "com vat"))
                    {
                        worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Com VAT";
                        indexColumnHeader++;
                    }

                    if (listColumns.Any(a => a.MenuSubName.ToLower() == "net receive from bank"))
                    {
                        worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Net Receive from Bank";
                        indexColumnHeader++;
                    }

                    if (listColumns.Any(a => a.MenuSubName.ToLower() == "com wht"))
                    {
                        worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Com WHT";
                        indexColumnHeader++;
                    }

                    if (listColumns.Any(a => a.MenuSubName.ToLower() == "net amount wht"))
                    {
                        worksheet.Cells[indexRowHeader, indexColumnHeader].Value = "Net Amount WHT";
                        indexColumnHeader++;
                    }

                    #endregion

                    // Fill the worksheet with data
                    int defaultStartRows = 2;
                    object obj = new object();
                    Parallel.ForEach(resultData, (transaction, state, index) =>
                    {
                        lock (obj)
                        {
                            int rowIndex = (int)index + defaultStartRows;
                            int columnIndex = 18;
                            //"Service Type";
                            worksheet.Cells[rowIndex, 1].Value = transaction.merchantServiceType == 1 ? "PF" : "PPAS";
                            //"Create Date Time";
                            worksheet.Cells[rowIndex, 2].Value = transaction.transactionDate;
                            //"Authorize Date Time";
                            worksheet.Cells[rowIndex, 3].Value = transaction.paidAt;
                            // "Merchant ID - Parent";
                            worksheet.Cells[rowIndex, 4].Value = transaction.mainBranchId;
                            //"Merchant ID";
                            worksheet.Cells[rowIndex, 5].Value = transaction.merchantCode;
                            //"Merchant Name";
                            worksheet.Cells[rowIndex, 6].Value = transaction.merchantName;
                            //"SAP Customer ID";
                            worksheet.Cells[rowIndex, 7].Value = transaction.sapCustomerId;
                            //"Business Type";
                            worksheet.Cells[rowIndex, 8].Value = transaction.merchantCategoryName;
                            // "SAP Invoice Ref";
                            worksheet.Cells[rowIndex, 9].Value = transaction.invoiceRef;
                            //"Transaction No";
                            worksheet.Cells[rowIndex, 10].Value = transaction.transactionNo;
                            //Provider Transaction Id
                            worksheet.Cells[rowIndex, 11].Value = transaction.chargeId;
                            //"Description";
                            worksheet.Cells[rowIndex, 12].Value = transaction.description;
                            //"SAP Internal Order";
                            worksheet.Cells[rowIndex, 13].Value = transaction.internalOrder;
                            //"Merchant Order No";
                            worksheet.Cells[rowIndex, 14].Value = transaction.InvoiceNo;
                            //"Source of Fund";
                            worksheet.Cells[rowIndex, 15].Value = transaction.paymentChannel;
                            //"Pay Amount";
                            worksheet.Cells[rowIndex, 16].Value = TrimDecimal(transaction.amount);
                            //Paid Amount
                            worksheet.Cells[rowIndex, 17].Value = TrimDecimal(transaction.paidAmount);

                            if (listColumns.Any(a => a.MenuSubName.ToLower() == "fee amount"))
                            {
                                // "Fee Amount";
                                worksheet.Cells[rowIndex, columnIndex].Value = TrimDecimal(transaction.fee);
                                columnIndex++;
                            }

                            if (listColumns.Any(a => a.MenuSubName.ToLower() == "tax amount"))
                            {
                                //"Tax Amount";
                                worksheet.Cells[rowIndex, columnIndex].Value = TrimDecimal(transaction.vat);
                                columnIndex++;
                            }

                            if (listColumns.Any(a => a.MenuSubName.ToLower() == "wht"))
                            {
                                //"WHT";
                                worksheet.Cells[rowIndex, columnIndex].Value = TrimDecimal(transaction.withHoldingTax);
                                columnIndex++;
                            }

                            if (listColumns.Any(a => a.MenuSubName.ToLower() == "net amount"))
                            {
                                //"Net Amount";
                                worksheet.Cells[rowIndex, columnIndex].Value = TrimDecimal(transaction.netAmount);
                                columnIndex++;
                            }

                            //"Payment Type";
                            worksheet.Cells[rowIndex, columnIndex].Value = transaction.source;
                            columnIndex++;
                            //"Payment Status";
                            worksheet.Cells[rowIndex, columnIndex].Value = transaction.transactionStatusName;
                            columnIndex++;
                            //"Provider Name";
                            worksheet.Cells[rowIndex, columnIndex].Value = transaction.providerName;
                            columnIndex++;
                            if (listColumns.Any(a => a.MenuSubName.ToLower() == "payback date"))
                            {
                                //"Payback Date";
                                worksheet.Cells[rowIndex, columnIndex].Value = transaction.transferDateTime;
                                columnIndex++;
                            }

                            if (listColumns.Any(a => a.MenuSubName.ToLower() == "transfer in from"))
                            {
                                //"Transfer in from";
                                worksheet.Cells[rowIndex, columnIndex].Value = transaction.issuer;
                                columnIndex++;
                            }

                            if (listColumns.Any(a => a.MenuSubName.ToLower() == "cash receive date"))
                            {
                                //"Cash Receive Date";
                                worksheet.Cells[rowIndex, columnIndex].Value = transaction.estimatedCashInDate;
                                columnIndex++;
                            }

                            if (listColumns.Any(a => a.MenuSubName.ToLower() == "reconcile report no"))
                            {
                                //"Reconcile Report No";
                                worksheet.Cells[rowIndex, columnIndex].Value = transaction.reconcileReportNo;
                                columnIndex++;
                            }

                            if (listColumns.Any(a => a.MenuSubName.ToLower() == "amount"))
                            {
                                //"Amount";
                                worksheet.Cells[rowIndex, columnIndex].Value = transaction.bahtAmount;
                                columnIndex++;
                            }

                            if (listColumns.Any(a => a.MenuSubName.ToLower() == "com amount"))
                            {
                                //"Com Amount";
                                worksheet.Cells[rowIndex, columnIndex].Value = transaction.bahtCommAmount;
                                columnIndex++;
                            }

                            if (listColumns.Any(a => a.MenuSubName.ToLower() == "com vat"))
                            {
                                //"Com VAT";
                                worksheet.Cells[rowIndex, columnIndex].Value = transaction.bahtVAT;
                                columnIndex++;
                            }

                            if (listColumns.Any(a => a.MenuSubName.ToLower() == "net receive from bank"))
                            {
                                //"Net Receive from Bank";
                                worksheet.Cells[rowIndex, columnIndex].Value = transaction.bahtNetAmount;
                                columnIndex++;
                            }

                            if (listColumns.Any(a => a.MenuSubName.ToLower() == "com wht"))
                            {
                                // "Com WHT";
                                worksheet.Cells[rowIndex, columnIndex].Value = transaction.wt;
                                columnIndex++;
                            }

                            if (listColumns.Any(a => a.MenuSubName.ToLower() == "net amount wht"))
                            {
                                // //"Net Amount WHT";
                                worksheet.Cells[rowIndex, columnIndex].Value = transaction.wtNetAmount;
                            }
                        }
                    });

                    stopwatch.Stop();
                    TimeSpan elapsedTime = stopwatch.Elapsed;
                    _logger.LogInformation($"CreateTransactionExcelFileList End Process Time : {elapsedTime}");
                    // Save the package to a byte array
                    return await Task.Run(() => package.GetAsByteArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"CreateTransactionExcelFileList Error : {JsonConvert.SerializeObject(ex)}");
                throw;
            }
        }

        private async Task<byte[]> CreateExcelMerchantTransaction(List<TransactionPagingDto> resultData)
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                _logger.LogInformation($"CreateTransactionExcelFileList Start");

                // Set the license context
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    // Create a worksheet
                    var worksheet = package.Workbook.Worksheets.Add("Transactions");

                    // Set the headers

                    #region | Set Header |

                    worksheet.Cells[1, 1].Value = "Create Date Time";
                    worksheet.Cells[1, 2].Value = "Authorize Date Time";
                    worksheet.Cells[1, 3].Value = "Merchant ID";
                    worksheet.Cells[1, 4].Value = "ร้านค้า";
                    worksheet.Cells[1, 5].Value = "Transaction No.";
                    worksheet.Cells[1, 6].Value = "Description";
                    worksheet.Cells[1, 7].Value = "Merchant Order No.";
                    worksheet.Cells[1, 8].Value = "ช่องทางการชำระเงิน";
                    worksheet.Cells[1, 9].Value = "จำนวนเงิน";
                    worksheet.Cells[1, 10].Value = "จำนวนเงินที่จ่าย";
                    worksheet.Cells[1, 11].Value = "ค่าบริการ";
                    worksheet.Cells[1, 12].Value = "Fee's VAT Amount";
                    worksheet.Cells[1, 13].Value = "เงินที่ได้รับ";
                    worksheet.Cells[1, 14].Value = "จุดรับชำระ";
                    worksheet.Cells[1, 15].Value = "สถานะ";

                    #endregion

                    // Fill the worksheet with data
                    int defaultStartRows = 2;
                    object obj = new object();
                    Parallel.ForEach(resultData, (transaction, state, index) =>
                    {
                        lock (obj)
                        {
                            int rowIndex = (int)index + defaultStartRows;
                            worksheet.Cells[rowIndex, 1].Value = transaction.transactionDate;
                            worksheet.Cells[rowIndex, 2].Value = transaction.paidAt;
                            worksheet.Cells[rowIndex, 3].Value = transaction.merchantCode;
                            worksheet.Cells[rowIndex, 4].Value = transaction.merchantName;
                            worksheet.Cells[rowIndex, 5].Value = transaction.transactionNo;
                            worksheet.Cells[rowIndex, 6].Value = transaction.description;
                            worksheet.Cells[rowIndex, 7].Value = transaction.InvoiceNo;
                            worksheet.Cells[rowIndex, 8].Value = transaction.paymentChannel.Contains("promptpay")
                                ? "promptPay"
                                : transaction.paymentChannel;
                            worksheet.Cells[rowIndex, 9].Value = transaction.amount;
                            worksheet.Cells[rowIndex, 10].Value = transaction.paidAmount;
                            worksheet.Cells[rowIndex, 11].Value = transaction.fee;
                            worksheet.Cells[rowIndex, 12].Value = transaction.vat;
                            worksheet.Cells[rowIndex, 13].Value = transaction.netAmount;
                            worksheet.Cells[rowIndex, 14].Value = transaction.source;
                            worksheet.Cells[rowIndex, 15].Value = transaction.transactionStatusName;
                        }
                    });

                    stopwatch.Stop();
                    TimeSpan elapsedTime = stopwatch.Elapsed;
                    _logger.LogInformation($"CreateTransactionExcelFileList End Process Time : {elapsedTime}");
                    // Save the package to a byte array
                    return await Task.Run(() => package.GetAsByteArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"CreateTransactionExcelFileList Error : {JsonConvert.SerializeObject(ex)}");
                throw;
            }
        }

        private string TrimDecimal(decimal? number)
        {
            if (!number.HasValue)
            {
                return string.Empty;
            }

            return number.Value.ToString("0.##");
        }

        #endregion
    }
}