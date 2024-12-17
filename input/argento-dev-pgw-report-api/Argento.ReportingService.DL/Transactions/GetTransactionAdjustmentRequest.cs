using Argento.ReportingService.DL.CustomAttributes;
using Argento.ReportingService.DL.Helpers;
using Argento.ReportingService.DL.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.DL.Transactions
{
    public class GetTransactionAdjustmentRequest : QueryStringParameters, IValidatableObject
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string ServiceType { get; set; }
        public string PaymentType { get; set; }
        [AllowPaymentChannelAttr]
        public List<string> PaymentChannels { get; set; }
        public List<string> TransactionStatus { get; set; }
        public string Keyword { get; set; }

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
                    results.Add(new ValidationResult("EndDate must be greater that startDate", new[] { "EndDate" }));
                }
            }

            return results;
        }
    }
}
