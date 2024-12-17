using Arcadia.Extensions.DependencyInjection.Attributes;
using Argento.ReportingService.BL.Interface;
using Argento.ReportingService.BL.Models;
using Argento.ReportingService.DL.Funding;
using Argento.ReportingService.DL.Transactions;
using Argento.ReportingService.Repository;
using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Repository.ReportingServiceDB;
using Argento.ReportingService.Utility;
using AutoMapper;
using AutoMapper.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Argento.ReportingService.BL.Service
{
    [RegisterType(typeof(IFundingService))]
    public class FundingService : IFundingService
    {
        private IUnitOfWorkReportingServiceDB unitOfWork;
        private readonly AppSettings appSettings;
        private IMapper mapper;
        private readonly DbContextReportingServiceDB _context;
        private readonly ILogger<FundingService> _logger;

        public FundingService(IOptions<AppSettings> appSettings
            , IMapper mapper
            , IUnitOfWorkReportingServiceDB unitOfWork
            , DbContextReportingServiceDB context
            , ILogger<FundingService> logger
        )
        {
            this.unitOfWork = unitOfWork;
            this.appSettings = appSettings.Value;
            this.mapper = mapper;
            this._context = context;
            _logger = logger;
        }

        public async Task ApproveTransaction(List<Guid> selectedTransactionIds, Guid userId)
        {
            var transactionRepository = unitOfWork.GetRepository<TransactionEntity>();
            var fundingHeaderRepository = unitOfWork.GetRepository<FundingHeadersEntity>();
            var fundingDetailRepository = unitOfWork.GetRepository<FundingDetailsEntity>();
            var approveDate = DateTime.UtcNow;
            var collection = await transactionRepository.GetAll().Where(x => selectedTransactionIds.Contains(x.Id)).ToListAsync();
            var merchantCollection = new Dictionary<Guid, FundingTransferData>();
            var maxNumber = 0;
            foreach (var tran in collection)
            {
                if (!tran.FundingDetailId.HasValue)
                {
                    merchantCollection.TryGetValue(tran.MerchantId, out var fundingData);

                    if (fundingData == null)
                    {
                        var reportNumber = "FT" + DateTime.UtcNow.ToString("yyyyMMdd");
                        var maxValue = fundingHeaderRepository.GetAll().Where(x => x.FundingTransferReportNo.Contains(reportNumber)).Max(x => x.FundingTransferReportNo);

                        if (string.IsNullOrEmpty(maxValue) && maxNumber == 0)
                        {
                            reportNumber += "0001";
                            maxNumber = 1;
                        }
                        else
                        {
                            // Get last 4 charactors
                            if (maxNumber == 0)
                            {
                                maxNumber = Convert.ToInt16(maxValue.Substring(maxValue.Length - 4));
                            }

                            maxNumber++;
                            reportNumber += maxNumber.ToString().PadLeft(4, '0');
                        }

                        var merchantRepository = this.unitOfWork.GetRepository<MerchantEntity>();
                        var accountRepository = this.unitOfWork.GetRepository<AccountEntity>();
                        var merchant = await merchantRepository.GetAll().Where(x => x.Id == tran.MerchantId).FirstOrDefaultAsync();
                        var accounts = await accountRepository.GetAll().Where(x => x.MerchantId == tran.MerchantId && !x.IsDeleted).ToListAsync();
                        AccountEntity filterAccount = new AccountEntity();
                        if (accounts.Any(x => x.IsPrimary))
                        {
                            filterAccount = accounts.Where(x => x.IsPrimary).FirstOrDefault();
                        }
                        else
                        {
                            throw new Exception($"[ERROR] Funding Transfer [Transaction] - {tran.Id} - Merchant [{merchant.MerchantName}] don't have primary account");
                        }

                        var funding = new FundingHeadersEntity()
                        {
                            Id = Guid.NewGuid(),
                            DueDateTime = tran.TransferDateTime.Value,
                            TotalTransactionInBatch = 1,
                            TotalAmount = tran.Balance,
                            FundingStatusId = Convert.ToInt16(FundingTransferType.PendingSettlement.Id),
                            FundingTransferReportNo = reportNumber,
                            MerchantId = tran.MerchantId,
                            MerchantCode = merchant.MerchantCode,
                            MerchantName = merchant.MerchantName,
                            SapCompanyCode = merchant.SapCustomerId,
                            BeneficiaryName = filterAccount.AccountName,
                            BeneficiaryEmail = merchant.Email,
                            BeneficiaryPhone = merchant.Phone,
                            BeneficiaryBankName = filterAccount.BankName,
                            BeneficiaryBankCode = filterAccount.BankCode,
                            BeneficiaryAccountNo = filterAccount.AccountNo,
                            BeneficiaryBankBranch = filterAccount.BankBranch,
                            SettlementDateTime = approveDate,
                        };

                        merchantCollection.Add(tran.MerchantId, new FundingTransferData { Details = new List<FundingDetailsEntity>(), Header = funding, Transactions = new List<TransactionEntity>() });
                    }
                    else
                    {

                        if (fundingData != null)
                        {
                            fundingData.Header.TotalTransactionInBatch++;
                            fundingData.Header.TotalAmount += tran.Balance;
                        }
                    }

                    if (fundingData == null)
                    {
                        merchantCollection.TryGetValue(tran.MerchantId, out fundingData);
                    }

                    var fundingDetail = new FundingDetailsEntity()
                    {
                        Id = Guid.NewGuid(),
                        DueDateTime = tran.TransferDateTime.Value,
                        Amount = tran.Balance,
                        FundingId = fundingData.Header.Id,
                        SettlementDateTime = approveDate,
                    };

                    fundingData.Details.Add(fundingDetail);

                    tran.FundingDetailId = fundingDetail.Id;
                    tran.ApproveStatusId = Convert.ToInt16(TransferApproveType.Approve);

                    fundingData.Transactions.Add(tran);
                }
                else
                {
                    throw new Exception($"[ERROR] Funding Transfer [Transaction] - {tran.Id} has been approved");
                }
            }

            if (merchantCollection.Any())
            {
                using (IDbContextTransaction trx = unitOfWork.BeginDbContextTransaction())
                {
                    Guid TransactionId = Guid.NewGuid();

                    try
                    {
                        // each merchantCollection
                        foreach (var merchant in merchantCollection)
                        {
                            var merchantData = merchant.Value;
                            var fundingHeader = merchantData.Header;
                            await fundingHeaderRepository.AddAsync(fundingHeader.Id, fundingHeader);
                            await fundingHeaderRepository.UnitOfWork.SaveChangesAsync();

                            await fundingDetailRepository.AddRangeAsync(fundingHeader.Id, merchantData.Details.ToArray());
                            await fundingDetailRepository.UnitOfWork.SaveChangesAsync();

                            await transactionRepository.UpdateRangeAsync(fundingHeader.Id, merchantData.Transactions.ToArray());
                            await transactionRepository.UnitOfWork.SaveChangesAsync();
                        }

                        trx.Commit();
                    }
                    catch (Exception ex)
                    {
                        trx.Rollback();

                        throw new Exception($"[ERROR] Funding Transfer message: {ex.Message}");
                    }
                }
            }
        }
    }
}

