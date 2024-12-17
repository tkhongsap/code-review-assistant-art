using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.DL.Transactions
{
    public enum TransactionOrderBy
    {
        transactionDate,
        merchantCode,
        merchantName,
        transactionNo,
        paymentChannel,

        amount,
        fee,
        vat,
        netAmount,
        source,

        transactionStatusName,
        invoiceRef,
        transferDateTime,
        invoiceNo,
        sapCustomerId,
        merchantCategoryName,
        mainBranchId,
        withHoldingTax,
        merchantServiceType,
        chargeId
    }
}
