using Argento.ReportingService.DL.FundingTransfer;
using Argento.ReportingService.DL.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.BL.Interface
{
    public interface IFundingTransferService
    {
        Task<PagedList<FundingTransferListDto>> Get(FundingTransferResourceParameter resourceParameter);
        Task<ExportRaw> GetExport(FundingTransferResourceParameter resourceParameter);
    }


    public class ExportRaw
    {
        public PagedList<ExportRawHeader> Header { get; set; }
        public PagedList<ExportRawDetail> Detail { get; set; }
    }

    public class ExportRawHeader
    {
        public string FileType { get; set; }
        public string RecordType { get; set; }
        public string BatchNumber { get; set; }
        public string SendingBankCode { get; set; }
        public string TotalTransactionInBatch { get; set; }
        public string TotalAmount { get; set; }
        public string EffectiveDate { get; set; }
        public string TransactionCode { get; set; }
        public string ReceiverNo { get; set; }
        public string CompanyId { get; set; }
        public string UserId { get; set; }
        public string Fillers { get; set; }
        public string CarriageReturnLineFeed { get; set; }
        public string ServiceType {  get; set; }
    }

    public class ExportRawDetail
    {
        public string FileType { get; set; }
        public string RecordType { get; set; }
        public string BatchNo { get; set; }
        public string ReceivingBank { get; set; }
        public string ReceivingAC { get; set; }
        public string SendingBankCode { get; set; }
        public string SendingBranchCode { get; set; }
        public string SendingAC { get; set; }
        public string EffectiveDate { get; set; }
        public string ServiceType { get; set; }
        public string ClearingHouseCode { get; set; }
        public string Amount { get; set; }
        public string ReceiverInfo { get; set; }
        public string ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public string SenderName { get; set; }
        public string OtherInfo1 { get; set; }
        public string DDARef1 { get; set; }
        public string RefNoDDARef2 { get; set; }
        public string ReserveField1 { get; set; }
        public string OtherInfo2 { get; set; }
        public string ReserveField2 { get; set; }

        public string RefRunningNumber { get; set; }
        public string Status { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string ReceivingSubBranchCode { get; set; }
        public string Fillers { get; set; }
        public string CarriageReturnLineFeed { get; set; }
    }
}
