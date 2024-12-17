using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Argento.ReportingService.Repository.Model
{
    public class MerchantEntity : MasterDataEntityBase
    {
        [Column(TypeName = "varchar(200)")]
        public string CallbackUrl { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string MerchantName { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string MerchantCode { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string ApiKey { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string Secret { get; set; }

        public Guid? MainBranchId { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string MainBranchName { get; set; }

        [Column(TypeName = "varchar(500)")]
        public string PaymentChannels { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string Banks { get; set; }

        [Column(TypeName = "varchar(500)")]
        public string Services { get; set; }
        public int PaymentTerm { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string MerchantKey { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string SecretKey { get; set; }

        [Column(TypeName = "varchar(100000)")]
        public string MdrRate { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string SapCustomerId { get; set; }
        public bool IsCompany { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Email { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string Phone { get; set; }

        [Column(TypeName = "varchar(36)")]
        public string MerchantCategoryId { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string MerchantCategoryName { get; set; }

        public int? MerchantServiceType { get; set; }
        [Column(TypeName = "character varying(3)")]
        public string CustomerGroup { get; set; }

    }
}
