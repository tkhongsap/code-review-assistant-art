using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Argento.ReportingService.Repository.Model
{
    public class AccountEntity : MasterDataEntityBase
    {
        public Guid MerchantId { get; set; }

        [Column(TypeName = "varchar(3)")]
        public string BankCode { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string BankName { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string BankBranch { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string AccountNo { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string AccountName { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string AccountTypeId { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string AccountTypeName { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }
    }
}
