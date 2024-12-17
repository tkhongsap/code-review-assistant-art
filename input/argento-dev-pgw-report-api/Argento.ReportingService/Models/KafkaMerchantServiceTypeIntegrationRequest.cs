using System;

namespace Argento.ReportingService.Models
{
    public class KafkaMerchantServiceTypeIntegrationRequest
    {
        public Guid MerchantId { get; set; }
        public int MerchantServiceType { get; set; }

        public KafkaMerchantServiceTypeIntegrationRequest(Guid merchantId, int merchantServiceType)
        {
            MerchantId = merchantId;
            MerchantServiceType = merchantServiceType;
        }
    }
}
