using Argento.ReportingService.DL.Helpers;
using Argento.ReportingService.DL.Transactions;
using Argento.ReportingService.Repository.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Argento.ReportingService.BL.Interface
{
    public interface ITransactionService
    {
        Task<List<TransactionEntity>> GetTransactionById(Guid id);
        Task<List<TransactionListDto>> GetTransactionByRequest(TransactionRequestDto requestDto, Guid merchantId);
        Task<TransactionResultPageDto> GetTransactionResult(Guid id);
        Task<PagedList<TransactionPagingDto>> GetTransactionByPaymentMerchant(Guid merchantId, TransactionPagingParameters resourceParameter);
        Task<byte[]> GetTransactionByPaymentMerchantExport(Guid merchantId, TransactionPagingParameters resourceParameter);
        Task<PagedList<TransactionPagingDto>> GetTransactionByPaymentAdmin(TransactionPagingParameters resourceParameter);
        Task<PagedList<TransactionPagingDto>> GetTransactionByPaymentAdminTransfer(TransactionPagingParameters resourceParameter);
        TransactionListDto GetTransactionByTransactionNo(Guid merchantId, string transactionNo);
        Task<TransactionDashboard> GetTransactionDashBoard(DashboardRequest resourceParameter);
        ValueTask SaveData(TransactionEntity dto);
        Task<byte[]> GetTransactionByPaymentAdminExcel(TransactionPagingParameters resourceParameter);
        Task TransactionAutoSendingReportExcel (TransactionAutoSendingReportrequest request);
        Task<PagedList<GetTransactionAdjustmentDto>> GetTransactionAdjustment(GetTransactionAdjustmentRequest request);

    }
}
