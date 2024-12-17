using Argento.ReportingService.DL.CustomAttributes;
using Argento.ReportingService.DL.Helpers;
using Argento.ReportingService.DL.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Argento.ReportingService.DL.Transactions
{
    public class TransactionRequestDto : QueryStringGetTransactionParameters, IValidatableObject
    {
        [RegularExpression(@"(\d){4}-(\d){2}-(\d){2}")]
        public string StartDate { get; set; }

        [RegularExpression(@"(\d){4}-(\d){2}-(\d){2}")]
        public string EndDate { get; set; }

        [AllowPaymentChannelAttr]
        public List<string> PaymentChannels { get; set; }
        public List<string> TransactionStatus { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> results = new List<ValidationResult>();

            if (!string.IsNullOrWhiteSpace(StartDate) && !string.IsNullOrWhiteSpace(EndDate))
            {
                DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                         $"{StartDate} 00:00:00", "yyyy-MM-dd HH:mm:ss");

                DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                            $"{EndDate} 23:59:59.997", "yyyy-MM-dd HH:mm:ss.fff");

                if (endDate <= startDate)
                {
                    results.Add(new ValidationResult("EndDate must be greater than startDate", new[] { "EndDate" }));
                }
            }

            if (Page <= 0)
            {
                results.Add(new ValidationResult("Page must be greater than 0"));
            }

            if (PageSize <= 0)
            {
                results.Add(new ValidationResult("PageSize must be greater than 0"));
            }

            return results;
        }
    }
}
