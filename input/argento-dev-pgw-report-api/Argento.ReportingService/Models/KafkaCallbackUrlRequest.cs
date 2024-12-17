using System;

namespace Argento.ReportingService.Models
{
    public class KafkaCallbackUrlRequest
    {
        public KafkaCallbackUrlRequest(Guid merchantId, string callbackUrl)
        {
            MerchantId = merchantId;
            CallbackUrl = callbackUrl;
        }

        public Guid MerchantId { get; set; }
        public string CallbackUrl { get; set; }
    }
}
