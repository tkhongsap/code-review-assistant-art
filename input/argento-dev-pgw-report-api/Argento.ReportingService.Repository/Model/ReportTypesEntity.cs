using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.Repository.Model
{
    public class ReportTypesEntity : MasterDataEntityBase
    {
        [Column(TypeName = "varchar(50)")]
        public string Name { get; set; }
    }
}
