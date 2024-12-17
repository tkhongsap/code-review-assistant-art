using Argento.ReportingService.DL.Transactions;
using Argento.ReportingService.Repository.Model;
using System.Linq;

namespace Argento.ReportingService.BL.Models
{
    public static class TransactionEntitySortable
    {
        public static IQueryable<TransactionEntity> CustomSort(this IQueryable<TransactionEntity> source, TransactionOrderBy orderBy, TransactionSortBy sortBy)
        {
            if (sortBy == TransactionSortBy.asc)
            {
                if (orderBy == TransactionOrderBy.transactionDate)
                {
                    source = source.OrderBy(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.merchantCode)
                {
                    source = source.OrderBy(x => x.MerchantCode).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.merchantName)
                {
                    source = source.OrderBy(x => x.MerchantName).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.transactionNo)
                {
                    source = source.OrderBy(x => x.TransactionNo).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.paymentChannel)
                {
                    source = source.OrderBy(x => x.PaymentChannel).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.amount)
                {
                    source = source.OrderBy(x => x.Amount).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.fee)
                {
                    source = source.OrderBy(x => x.Fee).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.vat)
                {
                    source = source.OrderBy(x => x.FeeVat).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.netAmount)
                {
                    source = source.OrderBy(x => x.Balance).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.source)
                {
                    source = source.OrderBy(x => x.SourceName).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.transactionStatusName)
                {
                    source = source.OrderBy(x => x.TransactionStatusId).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.invoiceRef)
                {
                    source = source.OrderBy(x => x.InvoiceRef).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.invoiceNo)
                {
                    source = source.OrderBy(x => x.InvoiceNo).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.transferDateTime)
                {
                    source = source.OrderBy(x => x.TransferDateTime).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.withHoldingTax)
                {
                    source = source.OrderBy(x => x.WithHoldingTax).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.merchantServiceType)
                {
                    source = source.OrderBy(x => x.MerchantServiceType).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.chargeId)
                {
                    source = source.OrderBy(x => x.ChargeId).ThenByDescending(x => x.CreatedTimestamp);
                }
                else { }
            }

            if (sortBy == TransactionSortBy.desc)
            {
                if (orderBy == TransactionOrderBy.transactionDate)
                {
                    source = source.OrderByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.merchantCode)
                {
                    source = source.OrderByDescending(x => x.MerchantCode).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.merchantName)
                {
                    source = source.OrderByDescending(x => x.MerchantName).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.transactionNo)
                {
                    source = source.OrderByDescending(x => x.TransactionNo).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.paymentChannel)
                {
                    source = source.OrderByDescending(x => x.PaymentChannel).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.amount)
                {
                    source = source.OrderByDescending(x => x.Amount).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.fee)
                {
                    source = source.OrderByDescending(x => x.Fee).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.vat)
                {
                    source = source.OrderByDescending(x => x.FeeVat).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.netAmount)
                {
                    source = source.OrderByDescending(x => x.Balance).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.source)
                {
                    source = source.OrderByDescending(x => x.SourceName).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.transactionStatusName)
                {
                    source = source.OrderByDescending(x => x.TransactionStatusId).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.invoiceRef)
                {
                    source = source.OrderByDescending(x => x.InvoiceRef).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.invoiceNo)
                {
                    source = source.OrderByDescending(x => x.InvoiceNo).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.transferDateTime)
                {
                    source = source.OrderByDescending(x => x.TransferDateTime).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.withHoldingTax)
                {
                    source = source.OrderByDescending(x => x.WithHoldingTax).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.merchantServiceType)
                {
                    source = source.OrderByDescending(x => x.MerchantServiceType).ThenByDescending(x => x.CreatedTimestamp);
                }
                else if (orderBy == TransactionOrderBy.chargeId)
                {
                    source = source.OrderByDescending(x => x.ChargeId).ThenByDescending(x => x.CreatedTimestamp);
                }
                else { }
            }

            return source;
        }
    }
}
