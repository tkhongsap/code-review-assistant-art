using Arcadia.Extensions.DependencyInjection.Attributes;
using Argento.ReportingService.BL.Interface;
using Argento.ReportingService.DL.Funding;
using Argento.ReportingService.DL.FundingTransfer;
using Argento.ReportingService.DL.Helpers;
using Argento.ReportingService.DL.Utils;
using Argento.ReportingService.Repository;
using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Repository.ReportingServiceDB;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Argento.ReportingService.BL.Service
{
    [RegisterType(typeof(IFundingTransferService))]
    public class FundingTransferService : IFundingTransferService
    {
        private readonly ILogger<FundingTransferService> _logger;
        private readonly DbContextReportingServiceDB _context;
        private IUnitOfWorkReportingServiceDB _unitOfWork;

        public FundingTransferService(ILogger<FundingTransferService> logger, IUnitOfWorkReportingServiceDB unitOfWork, DbContextReportingServiceDB context)
        {
            _logger = logger;
            _context = context;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedList<FundingTransferListDto>> Get(FundingTransferResourceParameter resourceParameter)
        {
            try
            {
                var detailRepo = _unitOfWork.GetRepository<FundingDetailsEntity>();
                var accountRepo = _unitOfWork.GetRepository<AccountEntity>();
                var fundingHeadersRepo = _unitOfWork.GetRepository<FundingHeadersEntity>();
                var header = fundingHeadersRepo.GetAll().Where(x => !x.IsDeleted);

                if (!string.IsNullOrWhiteSpace(resourceParameter.StartDate)
                    && !string.IsNullOrWhiteSpace(resourceParameter.EndDate))
                {

                    DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                                            $"{resourceParameter.StartDate} 00:00:00", "dd-MM-yyyy HH:mm:ss");
                    DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                                           $"{resourceParameter.EndDate} 23:59:59", "dd-MM-yyyy HH:mm:ss");

                    header = header.Where(x => x.SettlementDateTime >= startDate
                                                       && x.SettlementDateTime <= endDate);
                }

                if (resourceParameter.Bank != null && resourceParameter.Bank.Any())
                {
                    header = header.Where(x =>
                        resourceParameter.Bank.Contains(x.BeneficiaryBankCode));
                }

                if (resourceParameter.TransferStatus != null && resourceParameter.TransferStatus.Any())
                {
                    header = header.Where(x =>
                        resourceParameter.TransferStatus.Contains(x.FundingStatusId.ToString()));
                }

                var query = header.Select(
                    (a) => new FundingTransferListDto
                    {
                        DueDateTime = CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(a.DueDateTime, "dd/MM/yyyy HH:mm"),
                        PaymentDateTime = a.SettlementDateTime.HasValue
                            ? CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(a.SettlementDateTime.Value, "dd/MM/yyyy HH:mm")
                            : "",
                        MerchantId = a.MerchantId,
                        MerchantCode = a.MerchantCode,
                        MerchantName = a.MerchantName,
                        SapCompanyCode = a.SapCompanyCode,
                        BeneficiaryName = a.BeneficiaryName,
                        BeneficiaryBankName = a.BeneficiaryBankName,
                        BeneficiaryBankCode = a.BeneficiaryBankCode,
                        BeneficiaryAccountNo = a.BeneficiaryAccountNo,
                        Amount = a.TotalAmount,
                        TransferStatusId = a.FundingStatusId,
                        TransferStatusName = FundingTransferType.FromStatusId(a.FundingStatusId),
                        SlipUrl = null,
                        AccountName = a.BeneficiaryName,
                        BankName = a.BeneficiaryBankName,
                        BankCode = a.BeneficiaryBankCode,
                        BankBranch = a.BeneficiaryBankBranch,
                        AccountNo = a.BeneficiaryAccountNo,
                        FundingTransferReportNumber = a.FundingTransferReportNo,
                        BankStatus = null, // Waiting for other phase,
                        SapCustomerId = a.SapCompanyCode,
                        SettlementDateTime = CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(a.SettlementDateTime.Value, "dd/MM/yyyy HH:mm"),
                    }).OrderByDescending(x => x.FundingTransferReportNumber).AsQueryable();

                return await PagedList<FundingTransferListDto>.Create(query, resourceParameter.Page,
                    resourceParameter.PageSize);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task<ExportRaw> GetExport(FundingTransferResourceParameter resourceParameter)
        {
            return new ExportRaw
            {
                Header = null,
                Detail = null
            };
        }

    }


}
