namespace Argento.ReportingService.Utility
{
    public static partial class ArcadiaConstants
    {
        public static class ErrorCodes
        {
            public const string NullArgument = "NullArgument";
            public const string InputValidationFail = "InputValidationFail";
            public const string UsernameOrPasswordWrong = "UsernameOrPasswordWrong";
            public const string Unauthorized = "Unauthorized";

            public static class Database
            {
                public const string MismatchModifiedTimestamp = "MismatchModifiedTimestamp";
                public const string RecordNotExists = "DatabaseRecordNotExists";
                public const string InsertionFail = "DatabaseInsertionFail";
                public const string UpdateFail = "UpdateFail";
            }

            public const string InternalServerError = "InternalServerError";
        }
    }
}