using Argento.ReportingService.DL.Payments;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.DL.CustomAttributes
{
    public class AllowPaymentChannelAttr : ValidationAttribute
    {
        private readonly List<string> _allow = new List<string>();

        public AllowPaymentChannelAttr()
        {
            this._allow.Add(PaymentChannelType.PromptPay.Name);
            this._allow.Add(PaymentChannelType.VisaMasterCard_QR.Name);
            this._allow.Add(PaymentChannelType.AliPayCtoB.Name);
            this._allow.Add(PaymentChannelType.WeChatPayCtoB.Name);
            this._allow.Add(PaymentChannelType.CreditCard.Name);

            this._allow.Add(PaymentChannelType.ShopeePay.Name);
            this._allow.Add(PaymentChannelType.LinePayCtoB.Name);
            this._allow.Add(PaymentChannelType.PromptPayFlatRate.Name);
            this._allow.Add(PaymentChannelType.LinePayBtoC.Name);
            this._allow.Add(PaymentChannelType.TrueMoneyCtoBOffline.Name);

            this._allow.Add(PaymentChannelType.TrueMoneyBtoC.Name);
            this._allow.Add(PaymentChannelType.TrueMoneyCtoBOnline.Name);
            this._allow.Add(PaymentChannelType.AliPayBtoC.Name);
            this._allow.Add(PaymentChannelType.WeChatPayBtoC.Name);

            this._allow.Add(PaymentChannelType.ArtChainBtoC.Name);
            this._allow.Add(PaymentChannelType.ArtChainCtoB.Name);
            
            this._allow.Add(PaymentChannelType.TrueMoneyCtoB.Name);
            this._allow.Add(PaymentChannelType.TrueMoneyCtoB.Name);
            this._allow.Add(PaymentChannelType.MobileBankingKbankInternal.Name);
            this._allow.Add(PaymentChannelType.MobileBankingKbankExternal.Name);
            this._allow.Add(PaymentChannelType.MobileBankingBAY.Name);
            this._allow.Add(PaymentChannelType.MobileBankingKTBNext.Name);
            this._allow.Add(PaymentChannelType.MobileBankingKTBPaotang.Name);
        }
        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return true;
            }

            if (value is string input && _allow.Contains(input.ToLower()))
            {
                return true;
            }

            if (value is List<string> inputs)
            {
                var inputsConvert = ConvertStringArrayOrStringSplitToListString(inputs);

                foreach (var inputItem in inputsConvert)
                {
                    if (inputItem.Length == 0)
                    {
                        continue;
                    }

                    if (!_allow.Contains(inputItem.ToLower()))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        private List<string> ConvertStringArrayOrStringSplitToListString(List<string> input)
        {
            var ret = new List<string>();

            foreach (var inputItem in input)
            {
                if (string.IsNullOrWhiteSpace(inputItem))
                {
                    continue;
                }

                ret.AddRange(inputItem.Trim().ToLower().Split(',').ToList());
            }

            return ret;
        }
    }
}
