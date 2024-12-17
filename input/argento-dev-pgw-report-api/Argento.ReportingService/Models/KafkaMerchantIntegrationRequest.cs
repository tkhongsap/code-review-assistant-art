using System;

namespace Argento.ReportingService.Models
{
    public class KafkaMerchantIntegrationRequest
    {
        public Guid MerchantId { get; set; }
        public string MerchantName { get; set; }
        public string CallbackUrl { get; set; }
        public Guid? MainBranchId { get; set; }
        public string MainBranchName { get; set; }
        public string PaymentChannels { get; set; }
        public string Services { get; set; }
        public int PaymentTerm { get; set; }
        public string BankCode { get; }
        public string BankName { get; }
        public string BankImageUrl { get; }
        public string BankBranch { get; }
        public string AccountName { get; }
        public string AccountNo { get; }
        public string AccountTypeId { get; }
        public string AccountTypeName { get; }
        public string MerchantCode { get; set; }
        public string SapCustomerId { get; set; }
        public bool IsCompany { get; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string MerchantCategoryId { get; set; }
        public string MerchantCategoryName { get; set; }
        public string CustomerGroup { get; set; }
        public int? MerchantServiceType { get; set; }
        public Guid AccountId { get; set; }

        public KafkaMerchantIntegrationRequest(Guid merchantId, string merchantName, string merchantCode,
            string callbackUrl,
            Guid? mainBranchId, string mainBranchName, string paymentChannels,
            // string banks,
            string services,
            int paymentTerm,
            string bankCode, string bankName, string bankImageUrl, string bankBranch,
            string accountName, string accountNo, string accountTypeId, string accountTypeName, string sapCustomerId,
            bool isCompany, string email, string phone,
            string merchantCategoryId, string merchantCategoryName, string customerGroup, Guid accountId
        )
        {
            SapCustomerId = sapCustomerId;
            IsCompany = isCompany;
            MerchantId = merchantId;
            MerchantName = merchantName;
            MerchantCode = merchantCode;
            CallbackUrl = callbackUrl;
            MainBranchId = mainBranchId;
            MainBranchName = mainBranchName;
            PaymentChannels = paymentChannels;
            // Banks = banks;
            Services = services;
            PaymentTerm = paymentTerm;
            BankCode = bankCode;
            BankName = bankName;
            BankImageUrl = bankImageUrl;
            BankBranch = bankBranch;
            AccountName = accountName;
            AccountNo = accountNo;
            AccountTypeId = accountTypeId;
            AccountTypeName = accountTypeName;
            Email = email;
            Phone = phone;

            MerchantCategoryId = merchantCategoryId;
            MerchantCategoryName = merchantCategoryName;

            CustomerGroup = customerGroup;
            AccountId = accountId;
        }
    }
}
