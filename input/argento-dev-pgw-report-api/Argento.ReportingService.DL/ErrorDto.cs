using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Argento.ReportingService.DL
{
    public class ErrorDto
    {
        [JsonProperty("activityId")]
        public string ActivityId { get; set; }
        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }
        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
        [JsonProperty("errorPlace")]
        public string ErrorPlace { get; set; }
    }

    public class ErrorDetailDto
    {
        [JsonProperty("action")]
        public string Action { get; set; }
        [JsonProperty("level")]
        public string Level { get; set; }
        [JsonProperty("errors")]
        public Object Errors { get; set; }
    }
}
