using Arcadia.Extensions.DependencyInjection.Attributes;
using Argento.ReportingService.BL.Interface;
using Argento.ReportingService.Repository;
using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.BL.Service
{
    [RegisterType(typeof(IMerchantService))]
    public class MerchantService : IMerchantService
    {
        private IUnitOfWorkReportingServiceDB unitOfWork;
        private readonly AppSettings appSettings;

        public MerchantService(IOptions<AppSettings> appSettings
            , IUnitOfWorkReportingServiceDB unitOfWork)
        {
            this.unitOfWork = unitOfWork;
            this.appSettings = appSettings.Value;
        }

        public async Task<string> ValidateSecretKey(string secretKey)
        {
            try
            {
                var merchantRepo = unitOfWork.GetRepository<MerchantEntity>();
                var merchant = await merchantRepo.GetAll().Where(x => x.SecretKey == secretKey && !x.IsDeleted)
                                        .Select(x => new
                                        {
                                            x.Id
                                        })
                                        .FirstOrDefaultAsync();

                if (merchant == null)
                {
                    return "";
                }

                return merchant.Id.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Fail to select merchant code from database: " + ex.Message);
            }
        }
    }
}
