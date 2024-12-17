using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.BL.Interface
{
    public interface IMerchantService
    {
        Task<string> ValidateSecretKey(string secretKey);
    }
}
