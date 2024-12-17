using Arcadia.Extensions.DependencyInjection.Attributes;
using Argento.ReportingService.BL.Interface;
using Argento.ReportingService.DL;
using Argento.ReportingService.DL.Helpers;
using Argento.ReportingService.DL.Reconciles;
using Argento.ReportingService.DL.Transactions;
using Argento.ReportingService.Repository;
using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Utility;
using Argento.ReportingService.Utility.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static Argento.ReportingService.Utility.ArcadiaConstants;

namespace Argento.ReportingService.BL.Service
{
    [RegisterType(typeof(IReconcileService))]
    public class ReconcileService : IReconcileService
    {
        private readonly IUnitOfWorkReportingServiceDB _unitOfWork;

        public ReconcileService(IUnitOfWorkReportingServiceDB unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<Dropdown>> GetDropDownReportTypes()
        {
            try
            {
                var reportType = await _unitOfWork.GetRepository<ReportTypesEntity>().GetAll()
                                .Where(x => !x.IsDeleted)
                                .Select(x => new Dropdown
                                {
                                    Name = x.Name,
                                    Value = x.Id.ToString(),
                                    ImageUrl = ""
                                }).ToListAsync();

                return reportType;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fail to GetDropDownReportTypes: {ex.Message}");
            }
        }

        public async Task<ReconcileProcessResponse> CancelReconcileProcessDetail(ReconcileCancelRequest resourceParameter, string userId)
        {
            try
            {
                var reconcileProcessRepo = _unitOfWork.GetRepository<ReconcileProcessEntity>();
                var reconcileProcessDetailRepo = _unitOfWork.GetRepository<ReconcileProcessDetailsEntity>();
                var settlementReportDetailsRepo = _unitOfWork.GetRepository<SettlementReportDetailsEntity>();
                using (IDbContextTransaction trx = _unitOfWork.BeginDbContextTransaction())
                {
                    try
                    {
                        Guid reconcileProcessId = Guid.Parse(resourceParameter.reconcileProcessId);

                        var reconcileProcessEntity = reconcileProcessRepo.GetAll(true).FirstOrDefault(x => x.Id == reconcileProcessId);
                        if (reconcileProcessEntity != null)
                        {
                            reconcileProcessEntity.Remark = resourceParameter.remark;
                            reconcileProcessEntity.ProcessStatus = "Failed";
                            await reconcileProcessRepo.UpdateAsync(Guid.Parse(userId), reconcileProcessEntity);
                            await reconcileProcessRepo.UnitOfWork.SaveChangesAsync();
                        }
                        var reconcileProcessDetailEntity = reconcileProcessDetailRepo.GetAll(true).FirstOrDefault(x => x.ReconcileProcessId == reconcileProcessId);
                        if (reconcileProcessDetailEntity != null)
                        {
                            reconcileProcessDetailEntity.IsDeleted = true;
                            await reconcileProcessDetailRepo.DeleteAsync(Guid.Parse(userId), reconcileProcessDetailEntity);
                            await reconcileProcessDetailRepo.UnitOfWork.SaveChangesAsync();
                        }
                        var settleEntity = settlementReportDetailsRepo.GetAll(true).Where(x => x.ReconcileProcessId == reconcileProcessId).ToList();
                        if (settleEntity != null)
                        {
                            //settleEntity.IsDeleted = true;
                            settleEntity.ForEach(s => s.IsDeleted = true);
                            await settlementReportDetailsRepo.DeleteRangeAsync(Guid.Parse(userId), settleEntity.ToArray());
                            await settlementReportDetailsRepo.UnitOfWork.SaveChangesAsync();
                        }
                        trx.Commit();
                    }
                    catch (Exception ex)
                    {
                        return new ReconcileProcessResponse
                        {
                            respCode = "9999",
                            respDesc = ex.Message,
                        };
                    }
                }
                return new ReconcileProcessResponse
                {
                    respCode = "0000",
                    respDesc = "success",
                    respId = "",
                };
            }
            catch (Exception ex)
            {
                return new ReconcileProcessResponse
                {
                    respCode = "9999",
                    respDesc = ex.Message,
                };
            }
        }
        public async Task<PagedList<ReconcilePagingDto>> GetReconcileProcess(ReconcilePagingParameters resourceParameter)
        {
            try
            {

                var reconcileProcesses = _unitOfWork.GetRepository<ReconcileProcessEntity>().GetAll().Where(x => !x.IsDeleted);

                if (!string.IsNullOrWhiteSpace(resourceParameter.StartDate))
                {
                    DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                         $"{resourceParameter.StartDate} 00:00:00", "yyyy-MM-dd HH:mm:ss");

                    reconcileProcesses = reconcileProcesses.Where(x => x.CreatedTimestamp >= startDate);
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.EndDate))
                {
                    DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                        $"{resourceParameter.EndDate} 23:59:59.997", "yyyy-MM-dd HH:mm:ss.fff");

                    reconcileProcesses = reconcileProcesses.Where(x => x.CreatedTimestamp <= endDate);
                }

                if (resourceParameter.BankIssuer != null && resourceParameter.BankIssuer.Any())
                {
                    var BankIssuer = ConvertStringArrayOrStringSplitToListString(resourceParameter.BankIssuer);

                    if (BankIssuer.Count > 0)
                    {
                        reconcileProcesses = reconcileProcesses.Where(x => BankIssuer.Contains(x.Issuer));
                    }
                }

                if (resourceParameter.ReconcileStatus != null && resourceParameter.ReconcileStatus.Any())
                {
                    var ReconcileStatus = ConvertStringArrayOrStringSplitToListString(resourceParameter.ReconcileStatus);

                    if (ReconcileStatus.Count > 0)
                    {
                        reconcileProcesses = reconcileProcesses.Where(x => ReconcileStatus.Contains(x.ProcessStatus));
                    }
                }


                if (resourceParameter.ReportType != null && resourceParameter.ReportType.Any())
                {
                    var ReportType = ConvertStringArrayOrStringSplitToListString(resourceParameter.ReportType);

                    if (ReportType.Count > 0)
                    {
                        reconcileProcesses = reconcileProcesses.Where(x => ReportType.Contains(x.ReportTypeId.ToString()));
                    }
                }

                if (!string.IsNullOrWhiteSpace(resourceParameter.Keyword))
                {
                    reconcileProcesses = reconcileProcesses.Where(x => x.ReportFileName.Contains(resourceParameter.Keyword));
                }
                reconcileProcesses = reconcileProcesses.OrderByDescending(x => x.CreatedTimestamp);
                var query = reconcileProcesses.Join(_unitOfWork.GetRepository<ReportTypesEntity>().GetAll().Where(x => !x.IsDeleted), a => a.ReportTypeId, b => b.Id,
                (a, b) => new
                {
                    reconcileProcesses = a,
                    ReportTypeId = b.Id,
                    ReportTypeName = b.Name
                }).Join(_unitOfWork.GetRepository<BankEntity>().GetAll().Where(x => !x.IsDeleted), a => a.reconcileProcesses.Issuer, b => b.BankCode,
                (a, b) => new
                {
                    reconcileProcesses = a,
                    IssuerCode = b.BankCode,
                    IssuerName = b.BankName,
                }).Join(_unitOfWork.GetRepository<UserEntity>().GetAll().Where(x => !x.IsDeleted), a => a.reconcileProcesses.reconcileProcesses.CreatedByUserId, b => b.Id,
                (a, b) => new
                {
                    reconcileProcesses = a.reconcileProcesses,
                    IssuerCode = a.IssuerCode,
                    IssuerName = a.IssuerName,
                    ProcessBy = b.Firstname + " " + b.Lastname,
                }).Select(x => new ReconcilePagingDto
                {
                    ProcessfinishDateTime = x.reconcileProcesses.reconcileProcesses.CreatedTimestamp.HasValue ?
                        CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(x.reconcileProcesses.reconcileProcesses.CreatedTimestamp.Value, "yyyy-MM-dd HH:mm:ss") : "",
                    ReportFileName = x.reconcileProcesses.reconcileProcesses.ReportFileName,
                    ReportTypeName = x.reconcileProcesses.ReportTypeName,
                    Remark = x.reconcileProcesses.reconcileProcesses.Remark,
                    TotalAmount = x.reconcileProcesses.reconcileProcesses.TotalAmount,
                    TotalRecord = x.reconcileProcesses.reconcileProcesses.TotalRecord,
                    IssuerCode = x.IssuerCode,
                    IssuerName = x.IssuerName,
                    ProcessBy = x.ProcessBy,
                    ProcessStatus = x.reconcileProcesses.reconcileProcesses.ProcessStatus,
                    ReconcileReportNo = x.reconcileProcesses.reconcileProcesses.ReportNo,
                });

                return await PagedList<ReconcilePagingDto>.ToPagedList(query, resourceParameter.Page, resourceParameter.PageSize);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fail to GetDropDownReportTypes: {ex.Message}");
            }
        }

        public async Task<ReconcileLastestProcess> GetLastReconcileProcess(string bankIssuer, string filename)
        {
            try
            {
                var reconcileProcesses = await _unitOfWork.GetRepository<ReconcileProcessEntity>().GetAll()
                    .Where(x => !x.IsDeleted && bankIssuer.Contains(x.Issuer) && x.ProcessStatus == "Success" && x.ReportFileName == filename)
                    .OrderByDescending(x => x.CreatedTimestamp)
                    .Select(x => new ReconcileLastestProcess
                    {
                        ProcessfinishDateTime = x.CreatedTimestamp.HasValue ?
                            CustomStringDatetime.ConvertDateTimeUTCtoBangkokString(x.CreatedTimestamp.Value, "yyyy-MM-dd HH:mm") : "",
                        ProcessStatus = x.ProcessStatus,
                        TotalAmount = x.TotalAmount,
                        TotalRecord = x.TotalRecord,
                    }).FirstOrDefaultAsync();

                return reconcileProcesses ?? new ReconcileLastestProcess();
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

                ret.AddRange(inputItem.Trim().Split(',').ToList());
            }

            return ret;
        }

        private string GenerateReconcileReportNo(string maxReportNo)
        {
            const string prefix = "ST";
            int runningNumber;

            if (string.IsNullOrEmpty(maxReportNo))
            {
                runningNumber = 1;
            }
            else
            {
                // Extract the running number from the provided string
                runningNumber = int.Parse(maxReportNo.Substring(prefix.Length + 8));
                runningNumber++;
            }

            // Format the date part
            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
            DateTime targetLocalTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, targetTimeZone);
            string dateString = targetLocalTime.ToString("yyyyMMdd");

            // Format the running number with leading zeros
            string runningNumberString = runningNumber.ToString("D5");

            return $"{prefix}{dateString}{runningNumberString}";
        }

        public async Task<ReconcileProcessResponse> CheckTransactionApprove(List<SettlementReportDetails> list)
        {
            var listTransactionNo = list.Select(a => a.ReferenceOrder).ToList();
            var transactionRepo = _unitOfWork.GetRepository<TransactionEntity>();
            var listTransaction = transactionRepo.GetAll(false).Where(a => listTransactionNo.Contains(a.TransactionNo) && !a.IsDeleted && a.ApproveStatusId == Convert.ToInt16(TransferApproveType.Approve)).ToList();
            if (listTransaction.Any())
            {
                string tranApprove = string.Join(System.Environment.NewLine, listTransaction.Select(a => a.TransactionNo).ToArray());
                return new ReconcileProcessResponse
                {
                    respCode = "9999",
                    respDesc = $"Found an approved item - Please check data as follows \r\n{tranApprove}",
                };
            }
            return new ReconcileProcessResponse
            {
                respCode = "0000",
                respDesc = "success",
                respId = "",
            };
        }

        public async Task<ReconcileProcessResponse> CheckTransactionApproveFromFile(string fileUrl)
        {
            // Read the file content from the provided URL
            try
            {
                string fileContent = await GetFileContent(fileUrl);

                // Process the list similarly to CheckTransactionApprove
                var listTransactionNo = ParseTransactionNo(fileContent);
                var transactionRepo = _unitOfWork.GetRepository<TransactionEntity>();
                var listTransaction = transactionRepo.GetAll(false).Where(a => listTransactionNo.Contains(a.TransactionNo) && !a.IsDeleted && a.ApproveStatusId == Convert.ToInt16(TransferApproveType.Approve)).ToList();
                if (listTransaction.Any())
                {
                    string tranApprove = string.Join(Environment.NewLine, listTransaction.Select(a => a.TransactionNo).ToArray());
                    return new ReconcileProcessResponse
                    {
                        respCode = "9999",
                        respDesc = $"Found an approved item - Please check data as follows \r\n{tranApprove}",
                    };
                }
                return new ReconcileProcessResponse
                {
                    respCode = "0000",
                    respDesc = "success",
                    respId = "",
                };
            }
            catch (Exception ex)
            {
                return new ReconcileProcessResponse
                {
                    respCode = "9999",
                    respDesc = ex.Message,
                };
            }
        }

        public async Task<ReconcileProcessValidateFileResponse> ValidateFileFormat(string fileUrl)
        {
            List<string> errorList = new List<string>();
            var reconcileDataList = new List<ReconcileFileData>();
            decimal totalAmount = 0;

            var response = new ReconcileProcessValidateFileResponse
            {
                IsValid = false,
                Errors = new List<string>(),
                TotalAmount = 0,
                TotalRecord = 0
            };
            try
            {
                string fileContent = await GetFileContent(fileUrl);
                reconcileDataList = ParseFileContent(fileContent);
                if (reconcileDataList.Count() == 0)
                {
                    errorList.Add("No data found in the file");
                    response.IsValid = false;
                    response.Errors = errorList;
                    return response;
                }
            }
            catch (Exception ex)
            {
                errorList.Add($"Error parsing file: {ex.Message}");
                response.IsValid = false;
                response.Errors = errorList;
                return response;
            }

            var totalRecord = reconcileDataList.Count();
            var errorHeaders = ValidateFileHeader(reconcileDataList.FirstOrDefault());

            if (errorHeaders.Any())
            {
                errorList.AddRange(errorHeaders);
            }

            // check column TransactionNo is duplicate
            var duplicateTransactionNo = GetDuplicateTransaction(reconcileDataList.Select(x => x.TransactionNo).ToList());
            if (duplicateTransactionNo.Any())
            {
                errorList.Add($"    TransactionNo is duplicate : {string.Join(", ", duplicateTransactionNo)}");
            }


            for (int i = 1; i < totalRecord; i++)
            {
                var isRowValid = true;
                var reconcileDataItem = reconcileDataList[i];

                // Validate TransactionNo is not empty and 17 characters
                if (string.IsNullOrEmpty(reconcileDataItem.TransactionNo.Trim()) || reconcileDataItem.TransactionNo.Length != 17)
                {
                    errorList.Add($"▸ Row {i + 1}");
                    errorList.Add(
                      $"    {ReconcileReportConstants.TransactionNo} is require, is 17 characters"
                    );
                    isRowValid = false;
                }

                List<string> columnMessages = new List<string>
                {
                    // Validate the columns 2-7 is not empty and number
                    ValidateColumnNumber(reconcileDataItem.TotalAmount, ReconcileReportConstants.TotalAmount),
                    ValidateColumnNumber(reconcileDataItem.CommissionAmount, ReconcileReportConstants.CommissionAmount),
                    ValidateColumnNumber(reconcileDataItem.ComVATAmount, ReconcileReportConstants.ComVATAmount),
                    ValidateColumnNumber(reconcileDataItem.NetReceiveFromBank, ReconcileReportConstants.NetReceiveFromBank),
                    ValidateColumnNumber(reconcileDataItem.WT, ReconcileReportConstants.WT),
                    ValidateColumnNumber(reconcileDataItem.NetAmountAfterWHT, ReconcileReportConstants.NetAmountAfterWHT),

                    // validate column 8,9 is datetime with format DD-MM-YY HH:MM:SS or DD-MM-YY HH:MM
                    ValidateColumnDateTime(reconcileDataItem.CreateDateTime, ReconcileReportConstants.CreateDateTime),
                    ValidateColumnDateTime(reconcileDataItem.AuthDateTime, ReconcileReportConstants.AuthDateTime)
                };

                if (columnMessages.Where(x => x != null).Any())
                {
                    if (isRowValid)
                    {
                        errorList.Add($"▸ Row {i + 1}");
                        isRowValid = false;
                    }
                    columnMessages.Where(x => x != null).ToList().ForEach(x => errorList.Add(x));
                }

                if (!errorList.Any())
                {
                    totalAmount += decimal.Parse(reconcileDataItem.NetAmountAfterWHT);
                }
            }

            response.IsValid = !errorList.Any();
            response.Errors = errorList;
            response.TotalAmount = totalAmount;
            response.TotalRecord = totalRecord - 1;
            return response;
        }

        public async Task<ReconcileProcessResponse> SaveReconcileProcessFromFile(ReconcileProcessSaveFromFileRequest requestData, string userId)
        {
            try
            {
                string reportDate = requestData.FileName.Split("_")[3].Replace(".csv", "");
                string fileContent = await GetFileContent(requestData.FileUrl);
                var details = ParseFileToSettlementDetail(fileContent, reportDate);

                var reconcileProcessRepo = _unitOfWork.GetRepository<ReconcileProcessEntity>();
                var reconcileProcessDetailRepo = _unitOfWork.GetRepository<ReconcileProcessDetailsEntity>();
                var settlementReportDetailsRepo = _unitOfWork.GetRepository<SettlementReportDetailsEntity>();
                var transactionRepo = _unitOfWork.GetRepository<TransactionEntity>();

                string reconcileProcessId = "";

                Guid newReportTypeId = Guid.Parse(requestData.ReportTypeId);
                using (IDbContextTransaction trx = _unitOfWork.BeginDbContextTransaction())
                {
                    ReconcileProcessEntity reconcileEntity = new ReconcileProcessEntity
                    {
                        ReportFileName = requestData.FileName,
                        Remark = string.Empty,
                        ReportFileUrl = requestData.FileUrl,
                        ReportTypeId = newReportTypeId,
                        TotalAmount = requestData.TotalAmount,
                        TotalRecord = requestData.TotalRecord,
                        Issuer = requestData.Issuer,
                        ProcessStatus = "Success",
                        CreatedByUserId = Guid.Parse(userId),
                        LastModifiedByUserId = Guid.Parse(userId)
                    };

                    DateTime utcNow = DateTime.UtcNow;
                    TimeZoneInfo targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
                    DateTime targetLocalTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, targetTimeZone);
                    if (targetLocalTime.Kind == DateTimeKind.Unspecified)
                    {
                        targetLocalTime = DateTime.SpecifyKind(targetLocalTime, DateTimeKind.Utc).ToUniversalTime();
                    }

                    var maxReportNo = reconcileProcessRepo.GetAll(false).Where(x => x.ProcessStatus == "Success" && x.CreatedTimestamp.HasValue && x.CreatedTimestamp.Value.Day == targetLocalTime.Day && x.CreatedTimestamp.Value.Month == targetLocalTime.Month && x.CreatedTimestamp.Value.Year == targetLocalTime.Year).Max(x => x.ReportNo);

                    // Generate ReportNo STYYYYMMDDRRRRR
                    var reportNo = this.GenerateReconcileReportNo(maxReportNo);
                    reconcileEntity.ReportNo = reportNo;

                    await reconcileProcessRepo.AddAsync(userId, reconcileEntity);
                    await reconcileProcessRepo.UnitOfWork.SaveChangesAsync();

                    reconcileProcessId = reconcileEntity.Id.ToString();

                    Guid newReconcileProcessId = Guid.Parse(reconcileProcessId);
                    List<ReconcileProcessDetailsEntity> reconcileDetailList = new List<ReconcileProcessDetailsEntity>();
                    requestData.Details.ForEach(e =>
                    {
                        ReconcileProcessDetailsEntity reconcileDetailEntity = new ReconcileProcessDetailsEntity();
                        reconcileDetailEntity.PaymentMethod = e.PaymentMethod;
                        reconcileDetailEntity.ReconcileProcessId = newReconcileProcessId;
                        reconcileDetailEntity.EstimatedCashInDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                     $"{e.EstimatedCashInDate.Value:yyyy-MM-dd} 00:00:00", "yyyy-MM-dd HH:mm:ss");
                        reconcileDetailEntity.CreatedByUserId = Guid.Parse(userId);
                        reconcileDetailEntity.LastModifiedByUserId = Guid.Parse(userId);
                        reconcileDetailList.Add(reconcileDetailEntity);
                    });

                    await reconcileProcessDetailRepo.AddRangeAsync(userId, reconcileDetailList.ToArray());
                    await reconcileProcessDetailRepo.UnitOfWork.SaveChangesAsync();
                    var transactionNoList = details.Select(x => x.ReferenceOrder).ToList();

                    List<SettlementReportDetailsEntity> settlementDetailsList = new List<SettlementReportDetailsEntity>();
                    List<SettlementReportDetailsEntity> settlementDelete = new List<SettlementReportDetailsEntity>();
                    // elaspse time
                    var settlementOldData = settlementReportDetailsRepo.GetAll(true).Where(x => !x.IsDeleted && transactionNoList.Contains(x.ReferenceOrder)).ToList();
                    settlementOldData.ForEach(x => x.IsDeleted = true);
                    settlementDelete.AddRange(settlementOldData);
                    details.ForEach(e =>
                    {
                        SettlementReportDetailsEntity settlementDetailsEntity = new SettlementReportDetailsEntity();
                        settlementDetailsEntity.ReconcileProcessId = newReconcileProcessId;
                        settlementDetailsEntity.ReferenceOrder = e.ReferenceOrder.Trim();
                        settlementDetailsEntity.ReportDate = !e.ReportDate.HasValue ? null : CustomStringDatetime.ConvertStringToDateTimeUTC(
                                    $"{e.ReportDate.Value:yyyy-MM-dd} 00:00:00", "yyyy-MM-dd HH:mm:ss");
                        settlementDetailsEntity.ExRate = e.ExRate;
                        settlementDetailsEntity.BahtAmount = e.BahtAmount;
                        settlementDetailsEntity.BahtVAT = e.BahtVAT;
                        settlementDetailsEntity.BahtCommAmount = e.BahtCommAmount;
                        settlementDetailsEntity.WHT = e.WithHoldingTax == 0 ? decimal.Round(e.BahtCommAmount * (decimal)0.03, 4, MidpointRounding.AwayFromZero) : e.WithHoldingTax;
                        settlementDetailsEntity.BahtNetAmount = e.BahtNetAmount;
                        settlementDetailsEntity.ChargeDateTime = !e.ChargeDateTime.HasValue ? null : CustomStringDatetime.ConvertStringToDateTimeUTC(
                                    $"{e.ChargeDateTime.Value:yyyy-MM-dd HH:mm:ss}", "yyyy-MM-dd HH:mm:ss");
                        settlementDetailsEntity.AuthDateTime = !e.AuthDateTime.HasValue ? null : CustomStringDatetime.ConvertStringToDateTimeUTC(
                                    $"{e.AuthDateTime.Value:yyyy-MM-dd HH:mm:ss}", "yyyy-MM-dd HH:mm:ss");
                        settlementDetailsEntity.CurrencyCode = e.CurrencyCode;
                        settlementDetailsEntity.PaymentMethod = e.PaymentMethod.ToLower() == "linepay" ? "line pay (c scan b)" : e.PaymentMethod.ToLower() == "truemoney" ? "truemoney (c scan b)" : e.PaymentMethod;
                        settlementDetailsEntity.PaymentType = e.PaymentType;
                        settlementDetailsEntity.SourceOfFund = e.SourceOfFund;
                        settlementDetailsEntity.CreatedByUserId = Guid.Parse(userId);
                        settlementDetailsEntity.LastModifiedByUserId = Guid.Parse(userId);
                        settlementDetailsEntity.BahtNetWHTAmount = e.NetWithHoldingTax;
                        settlementDetailsList.Add(settlementDetailsEntity);
                    });

                    var transactions = transactionRepo.GetAll(false).Where(x => transactionNoList.Contains(x.TransactionNo)).ToList();
                    transactions.ForEach(x => x.ReconcileStatus = ReconcileStatus.Success);

                    await settlementReportDetailsRepo.AddRangeAsync(userId, settlementDetailsList.ToArray());
                    await settlementReportDetailsRepo.UnitOfWork.SaveChangesAsync();
                    await settlementReportDetailsRepo.DeleteRangeAsync(Guid.Parse(userId), settlementDelete.ToArray()).ConfigureAwait(false);
                    await settlementReportDetailsRepo.UnitOfWork.SaveChangesAsync();
                    await transactionRepo.UpdateRangeAsync(userId, transactions.ToArray()).ConfigureAwait(false);
                    await transactionRepo.UnitOfWork.SaveChangesAsync();

                    trx.Commit();
                }

                return new ReconcileProcessResponse
                {
                    respCode = "0000",
                    respDesc = "success",
                    respId = reconcileProcessId,
                };
            }
            catch (Exception ex)
            {
                return new ReconcileProcessResponse
                {
                    respCode = "9999",
                    respDesc = "Fail to save reconcile process from file: " + ex.Message,
                };
            }
        }

        private List<ReconcileFileData> ParseFileContent(string fileContent)
        {
            var result = new List<ReconcileFileData>();
            using (var reader = new StringReader(fileContent))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var values = line.Split(',');
                    if (values.Length > 0)
                    {
                        var data = new ReconcileFileData
                        {
                            TransactionNo = values[0],
                            TotalAmount = values[1],
                            CommissionAmount = values[2],
                            ComVATAmount = values[3],
                            NetReceiveFromBank = values[4],
                            WT = values[5],
                            NetAmountAfterWHT = values[6],
                            CreateDateTime = values[7],
                            AuthDateTime = values[8]
                        };
                        result.Add(data);
                    }
                }
            }
            return result;
        }

        private List<string> GetDuplicateTransaction(List<string> list)
        {
            return list.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
        }

        private List<SettlementReportDetails> ParseFileToSettlementDetail(string fileContent, string reportDate)
        {
            var result = new List<SettlementReportDetails>();
            using (var reader = new StringReader(fileContent))
            {
                string line;
                bool isHeader = true;
                while ((line = reader.ReadLine()) != null)
                {
                    if (isHeader)
                    {
                        isHeader = false;
                        continue; // Skip the header line
                    }
                    var values = line.Split(',');
                    if (values.Length > 0)
                    {
                        string chargeFormat = values[7].Split(' ')[1].Split(':').Length == 2 ? "dd-MM-yy HH:mm" : "dd-MM-yy HH:mm:ss";
                        string authDateTime = values[8].Split(' ')[1].Split(':').Length == 2 ? "dd-MM-yy HH:mm" : "dd-MM-yy HH:mm:ss";

                        var data = new SettlementReportDetails
                        {
                            ReferenceOrder = values[0],
                            BahtAmount = Convert.ToDecimal(values[1]),
                            BahtCommAmount = Convert.ToDecimal(values[2]),
                            BahtVAT = Convert.ToDecimal(values[3]),
                            BahtNetAmount = Convert.ToDecimal(values[4]),
                            WithHoldingTax = Convert.ToDecimal(values[5]),
                            NetWithHoldingTax = Convert.ToDecimal(values[6]),
                            ChargeDateTime = CustomStringDatetime.ConvertStringToDateTimeUTC(
                                $"{values[7]}", chargeFormat),
                            AuthDateTime = CustomStringDatetime.ConvertStringToDateTimeUTC(
                                $"{values[8]}", authDateTime),
                            SourceOfFund = "ThaiQR",
                            PaymentMethod = "qr",
                            PaymentType = string.Empty,
                            ReportDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                                $"{reportDate} 00:00:00", "ddMMyyyy HH:mm:ss"),
                            CurrencyCode = "THB",
                            ExRate = 1,
                        };

                        result.Add(data);
                    }
                }
            }
            return result;
        }

        private List<string> ValidateFileHeader(ReconcileFileData reconcileFileData)
        {
            List<string> errorList = new List<string>();

            if (reconcileFileData.TransactionNo != ReconcileReportConstants.TransactionNo)
            {
                errorList.Add("Invalid file header: Column 1 - Transaction No");
            }

            if (reconcileFileData.TotalAmount != ReconcileReportConstants.TotalAmount)
            {
                errorList.Add("Invalid file header: Column 2 - Total Amount");
            }

            if (reconcileFileData.CommissionAmount != ReconcileReportConstants.CommissionAmount)
            {
                errorList.Add("Invalid file header: Column 3 - Commission Amount");
            }

            if (reconcileFileData.ComVATAmount != ReconcileReportConstants.ComVATAmount)
            {
                errorList.Add("Invalid file header: Column 4 - Com's VAT Amount");
            }

            if (reconcileFileData.NetReceiveFromBank != ReconcileReportConstants.NetReceiveFromBank)
            {
                errorList.Add("Invalid file header: Column 5 - Net Receive from Bank");
            }

            if (reconcileFileData.WT != ReconcileReportConstants.WT)
            {
                errorList.Add("Invalid file header: Column 6 - W/T");
            }

            if (reconcileFileData.NetAmountAfterWHT != ReconcileReportConstants.NetAmountAfterWHT)
            {
                errorList.Add("Invalid file header: Column 7 - Net Amount after WHT");
            }

            if (reconcileFileData.CreateDateTime != ReconcileReportConstants.CreateDateTime)
            {
                errorList.Add("Invalid file header: Column 8 - Create Date Time");
            }

            if (reconcileFileData.AuthDateTime != ReconcileReportConstants.AuthDateTime)
            {
                errorList.Add("Invalid file header: Column 9 - Auth Date Time");
            }

            return errorList;
        }

        private string ValidateColumnNumber(string value, string columnName)
        {
            if (string.IsNullOrEmpty(value.Trim()) || !decimal.TryParse(value, out _))
            {
                return $"    {columnName} is require, is number";
            }
            return null;
        }

        private string ValidateColumnDateTime(string value, string columnName)
        {
            var formats = new[] { "dd-MM-yy HH:mm:ss", "dd-MM-yy HH:mm" };

            if (string.IsNullOrEmpty(value) || !DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return $"    {columnName} is invalid date format dd-MM-yy HH:mm:ss or dd-MM-YY HH:mm";
            }

            return null;
        }

        private List<string> ParseTransactionNo(string fileContent)
        {
            var result = new List<string>();
            using (var reader = new StringReader(fileContent))
            {
                string line;
                bool isHeader = true;
                while ((line = reader.ReadLine()) != null)
                {
                    if (isHeader)
                    {
                        isHeader = false;
                        continue; // Skip the header line
                    }
                    var values = line.Split(',');
                    if (values.Length > 0)
                    {
                        result.Add(values[0]); // Add the first column value
                    }
                }
            }
            return result;
        }

        private async Task<string> GetFileContent(string fileUrl)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    return await httpClient.GetStringAsync(fileUrl);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("[Cannot read file] - " + ex.Message);
            }
        }
    }
}