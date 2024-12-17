using System;

namespace Argento.ReportingService.Models
{
    public class KafkaAccountRequest
    {
        public Guid Id { get; set; }
        public Guid MerchantId { get; set; }
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public string BankBranch { get; set; }
        public string AccountNo { get; set; }
        public string AccountName { get; set; }
        public string AccountTypeId { get; set; }
        public string AccountTypeName { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }
        public Guid? LastModifiedByUserId { get; set; }
        public DateTime? LastModifiedTimestamp { get; set; }
        public Guid? DeletedByUserId { get; set; }
        public DateTime? DeletedTimestamp { get; set; }
        public bool IsDeleted { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTime? CreatedTimestamp { get; set; }
    }
}
