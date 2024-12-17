using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Argento.ReportingService.Repository.Model
{
    public class BankEntity : MasterDataEntityBase
    {
        [Column(TypeName = "varchar(3)")]
        public string BankCode { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string BankName { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string ImageUrl { get; set; }
    }
}
