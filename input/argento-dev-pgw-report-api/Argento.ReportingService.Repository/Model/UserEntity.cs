using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Argento.ReportingService.Repository.Model
{
    public class UserEntity : MasterDataEntityBase
    {
        [Column(TypeName = "varchar(256)")]
        public string UserName { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string Email { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string PhoneNumber { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string UserType { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string EmployeeCode { get; set; }
        [Column(TypeName = "varchar(200)")]
        public string Firstname { get; set; }
        [Column(TypeName = "varchar(200)")]
        public string Lastname { get; set; }
        [Column(TypeName = "boolean")]
        public bool IsActive { get; set; }
        [Column(TypeName = "uuid")]
        public Guid MerchantId { get; set; }
        [Column(TypeName = "varchar(200)")]
        public string MerchantName { get; set; }
    }
}
