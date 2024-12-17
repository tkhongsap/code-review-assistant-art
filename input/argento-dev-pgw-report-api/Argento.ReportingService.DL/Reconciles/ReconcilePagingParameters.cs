using Argento.ReportingService.DL.Helpers;
using Argento.ReportingService.Utility.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.DL.Reconciles
{
    public class ReconcilePagingParameters : QueryStringParameters, IValidatableObject
    {
        [RegularExpression(@"(\d){4}-(\d){2}-(\d){2}")]
        public string StartDate { get; set; }

        [RegularExpression(@"(\d){4}-(\d){2}-(\d){2}")]
        public string EndDate { get; set; }
        public List<string> BankIssuer { get; set; }

        public List<string> ReconcileStatus { get; set; }
        public List<string> ReportType { get; set; }

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
