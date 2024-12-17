namespace Argento.ReportingService.Utility
{
    public static partial class ArcadiaConstants
    {
        public const string Yes = "Y";
        public const string No = "N";

        public static class UserIds
        {
            public const long Admin = 1; // The default and first user record in the Users table which is the "admin" account.
        }

        public static class RoleIds
        {
            public const long Admin = 1;
        }

        public static class Modes
        {
            public const int Inactive = 0;
            public const int Active = 1;
        }

        public static class HeaderKeys
        {
            public const string ErrorCode = "X-Error-Code";
            public const string ErrorMessage = "X-Error-Message";
            public const string ErrorPlace = "X-Error-Place";
            public const string ActivitId = "X-Activity-Id";
            public const string Authorization = "Authorization";

        }

        public static class RequestScopeKeys
        {
            public const string ActivityId = "ActivityId";
            public const string RequestedByUser = "RequestedByUser";
            public const string MerchantId = "MerchantId";
            public const string UserId = "UserId";
        }

        public static class Database
        {
            public const int IdMaxLength = 50;
            public const int ShortTextMaxLength = 255;
            public const int LongTextMaxLength = 2000;
            public const int DeletedFlagMaxLength = 1;
            public const int MiniTextMaxLength = 20;
        }

        public static class ExceptionLevel
        {
            public const string Information = "INFORMATION";
            public const string Error = "ERROR";
            public const string Warning = "WARNING";
        }
        public static class TransactionStatus
        {
            public const int WaitForTransfer = 1;
            public const int Paid = 2;
            public const int Cancel = 3;
            public const int Fail = 4;
            public const int NotFullyPaid = 5;
            public const int Void = 6;

            public static string FromStatusId(int id)
            {
                switch (id)
                {
                    case 1:
                        return "Wait for transfer";
                    case 2:
                        return "Paid";
                    case 3:
                        return "Cancel";
                    case 4:
                        return "Fail";
                    case 5:
                        return "Not fully paid";
                    case 6:
                        return "Void";
                    default:
                        return "";
                }
            }
        }
        public static class LoggerNames
        {
            public class Web
            {
            }

            public class Database
            {
            }

            public class Error
            {
            }
        }

        public static class ReconcileStatus
        {
            public const string Success = "SUCCESS";
            public const string Failed = "FAILED";
            public const string Cancel = "CANCELED";
        }
    }
}
