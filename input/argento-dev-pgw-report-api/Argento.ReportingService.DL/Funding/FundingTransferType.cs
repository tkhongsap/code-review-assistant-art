using System;
using System.Collections.Generic;
using System.Linq;

namespace Argento.ReportingService.DL.Funding
{
    public class FundingTransferType
            : Enumeration
    {
        public static FundingTransferType AccountApprove =
            new FundingTransferType("1", nameof(AccountApprove).ToLowerInvariant());

        public static FundingTransferType PendingSettlement =
            new FundingTransferType("2", nameof(PendingSettlement).ToLowerInvariant());

        public static FundingTransferType SentToBank =
            new FundingTransferType("3", nameof(SentToBank).ToLowerInvariant());

        public static FundingTransferType ResponseFromBank =
            new FundingTransferType("4", nameof(ResponseFromBank).ToLowerInvariant());

        public static FundingTransferType Success =
            new FundingTransferType("5", nameof(Success).ToLowerInvariant());

        public static FundingTransferType Reject =
            new FundingTransferType("6", nameof(Reject).ToLowerInvariant());


        public FundingTransferType(string id, string name)
            : base(id, name)
        {
        }

        public static IEnumerable<FundingTransferType> List() =>
            new[]
            {
                AccountApprove, PendingSettlement, SentToBank,
                ResponseFromBank, Success, Reject
            };

        public static string FromStatusId(int id)
        {
            switch (id)
            {
                case 1:
                    return "Account Approve";
                case 2:
                    return "Pending for settle";
                case 3:
                    return "Sent to bank";
                case 4:
                    return "Response from bank";
                case 5:
                    return "Success";
                case 6:
                    return "Reject";
                default:
                    {
                        throw new Exception("Invalid Status");
                    }
            }
        }

        public static FundingTransferType FromName(string name)
        {
            var state = List()
                .SingleOrDefault(s => String.Equals(s.Name, name, StringComparison.CurrentCultureIgnoreCase));

            if (state == null)
            {
                throw new Exception("Invalid Status");
            }

            return state;
        }

        public static FundingTransferType From(string id)
        {
            var state = List().SingleOrDefault(s => s.Id == id);

            if (state == null)
            {
                throw new Exception(
                    $"Possible values for OrderStatus: {String.Join(",", List().Select(s => s.Name))}");
            }

            return state;
        }
    }
}
