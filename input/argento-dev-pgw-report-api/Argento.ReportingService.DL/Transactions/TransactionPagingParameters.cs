using Argento.ReportingService.DL.CustomAttributes;
using Argento.ReportingService.DL.Helpers;
using Argento.ReportingService.DL.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Argento.ReportingService.DL.Transactions
{
    public class TransactionPagingParameters : QueryStringParameters, IValidatableObject
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string StartPaybackDate { get; set; }
        public string EndPaybackDate { get; set; }
        public string StartEstimatedCashInDate { get; set; }
        public string EndEstimatedCashInDate { get; set; }

        [AllowPaymentChannelAttr]
        public List<string> PaymentChannels { get; set; }
        public List<string> Sources { get; set; }
        public List<string> TransactionStatus { get; set; }
        public List<string> MerchantServiceTypes { get; set; }
        public List<string> Bank { get; set; }
        public List<string> ApproveStatus { get; set; }

        public string Keyword { get; set; }

        [JsonPropertyName("OrderBy")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TransactionOrderBy OrderBy { get; set; }


        [JsonPropertyName("Order")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TransactionSortBy Order { get; set; }

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

            if (!string.IsNullOrWhiteSpace(StartPaybackDate) && !string.IsNullOrWhiteSpace(EndPaybackDate))
            {
                DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                         $"{StartPaybackDate} 00:00:00", "yyyy-MM-dd HH:mm:ss");

                DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                            $"{EndPaybackDate} 23:59:59.997", "yyyy-MM-dd HH:mm:ss.fff");

                if (endDate <= startDate)
                {
                    results.Add(new ValidationResult("endPaybackDate must be greater that startPaybackDate", new[] { "endPaybackDate" }));
                }
            }

            if (!string.IsNullOrWhiteSpace(StartEstimatedCashInDate) && !string.IsNullOrWhiteSpace(EndEstimatedCashInDate))
            {
                DateTime startDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                         $"{StartEstimatedCashInDate} 00:00:00", "yyyy-MM-dd HH:mm:ss");

                DateTime endDate = CustomStringDatetime.ConvertStringToDateTimeUTC(
                            $"{EndEstimatedCashInDate} 23:59:59.997", "yyyy-MM-dd HH:mm:ss.fff");

                if (endDate <= startDate)
                {
                    results.Add(new ValidationResult("endEstimatedCashInDate must be greater that startEstimatedCashInDate", new[] { "endEstimatedCashInDate" }));
                }
            }

            return results;
        }

        public Guid RoleId { get; set; }
    }
}
