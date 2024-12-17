using Argento.ReportingService.DL.Transactions;
using System.Linq;

namespace Argento.ReportingService.BL.Models
{
    public static class TransactionPagingDtoSortable
    {
        public static IQueryable<TransactionPagingDto> CustomSort(this IQueryable<TransactionPagingDto> source, TransactionOrderBy orderBy, TransactionSortBy sortBy)
        {
            if (sortBy == TransactionSortBy.asc)
            {
                if (orderBy == TransactionOrderBy.sapCustomerId)
                {
                    source = source.OrderBy(x => x.sapCustomerId);
                }
                else if (orderBy == TransactionOrderBy.merchantCategoryName)
                {
                    source = source.OrderBy(x => x.merchantCategoryName);
                }
                else if (orderBy == TransactionOrderBy.mainBranchId)
                {
                    source = source.OrderBy(x => x.mainBranchId);
                }
                else { }
            }

            if (sortBy == TransactionSortBy.desc)
            {
                if (orderBy == TransactionOrderBy.sapCustomerId)
                {
                    source = source.OrderByDescending(x => x.sapCustomerId);
                }
                else if (orderBy == TransactionOrderBy.merchantCategoryName)
                {
                    source = source.OrderByDescending(x => x.merchantCategoryName);
                }
                else if (orderBy == TransactionOrderBy.mainBranchId)
                {
                    source = source.OrderByDescending(x => x.mainBranchId);
                }
                else { }
            }

            return source;
        }
    }
}
