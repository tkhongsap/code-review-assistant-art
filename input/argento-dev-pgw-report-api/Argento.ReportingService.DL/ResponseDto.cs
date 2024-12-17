using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Argento.ReportingService.DL
{
    public class ResponseDto
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        [JsonProperty("data")]
        public Object Data { get; set; }
        [JsonProperty("activityid")]
        public string ActivityId { get; set; }
        [JsonProperty("error")]
        public Object Error { get; set; }
    }

    public class ResponseDto<T>
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        [JsonProperty("data")]
        public T Data { get; set; }
        [JsonProperty("activityid")]
        public string ActivityId { get; set; }
        [JsonProperty("error")]
        public ErrorDetailDto Error { get; set; }
    }
}
