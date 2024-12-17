using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.DL.Payments
{
    public class PaymentChannelType : Enumeration
    {
        public static PaymentChannelType PromptPay = new("1", "promptpay");
        public static PaymentChannelType VisaMasterCard_QR = new("2", "visa/mastercard (qr)");
        public static PaymentChannelType AliPayCtoB = new("3", "alipay (c scan b)");
        public static PaymentChannelType WeChatPayCtoB = new("4", "wechatpay (c scan b)");
        public static PaymentChannelType CreditCard = new("5", "creditcard");

        public static PaymentChannelType ShopeePay = new("6", "shopee pay");
        public static PaymentChannelType LinePayCtoB = new("7", "line pay (c scan b)");
        public static PaymentChannelType PromptPayFlatRate = new("8", "promptpay (flatrate)");
        public static PaymentChannelType LinePayBtoC = new("9", "line pay (b scan c)");
        public static PaymentChannelType TrueMoneyCtoBOffline = new("10", "truemoney (c scan b - offline)");
        public static PaymentChannelType TrueMoneyBtoC = new("11", "truemoney (b scan c)");
        public static PaymentChannelType TrueMoneyCtoBOnline = new("12", "truemoney (c scan b - online)");
        public static PaymentChannelType AliPayBtoC = new("13", "alipay (b scan c)");
        public static PaymentChannelType WeChatPayBtoC = new("14", "wechatpay (b scan c)");
        public static PaymentChannelType ArtChainCtoB = new("15", "art token (c scan b)");
        public static PaymentChannelType ArtChainBtoC = new("16", "art token (b scan c)");
        public static PaymentChannelType TrueMoneyCtoB = new("10,12", "truemoney (c scan b)");
         public static PaymentChannelType MobileBankingSCB = new("17", "mobile banking scb");
        public static PaymentChannelType MobileBankingKbankInternal = new("18", "mobile banking kbank internal");
        public static PaymentChannelType MobileBankingKTBPaotang = new("19", "mobile banking ktb paotang");
        public static PaymentChannelType MobileBankingKTBNext = new("20", "mobile banking ktb next");
        public static PaymentChannelType MobileBankingBAY = new("21", "mobile banking bay");
        public static PaymentChannelType MobileBankingKbankExternal = new("22", "mobile banking kbank external");


        public PaymentChannelType(string id, string name) : base(id, name)
        {
        }

        public static PaymentChannelType Convert(string id)
        {
            if (id == "1") { return PaymentChannelType.PromptPay; }
            if (id == "2") { return PaymentChannelType.VisaMasterCard_QR; }
            if (id == "3") { return PaymentChannelType.AliPayCtoB; }
            if (id == "4") { return PaymentChannelType.WeChatPayCtoB; }
            if (id == "5") { return PaymentChannelType.CreditCard; }
            if (id == "6") { return PaymentChannelType.ShopeePay; }
            if (id == "7") { return PaymentChannelType.LinePayCtoB; }
            if (id == "8") { return PaymentChannelType.PromptPayFlatRate; }
            if (id == "9") { return PaymentChannelType.LinePayBtoC; }
            if (id == "10") { return PaymentChannelType.TrueMoneyCtoBOffline; }
            if (id == "11") { return PaymentChannelType.TrueMoneyBtoC; }
            if (id == "12") { return PaymentChannelType.TrueMoneyCtoBOnline; }
            if (id == "13") { return PaymentChannelType.AliPayBtoC; }
            if (id == "14") { return PaymentChannelType.WeChatPayBtoC; }
            if (id == "15") { return PaymentChannelType.ArtChainCtoB; }
            if (id == "16") { return PaymentChannelType.ArtChainBtoC; }
            throw new Exception("Cannot Convert");
        }

        public static PaymentChannelType ConvertFromName(string name)
        {
            if (name == PaymentChannelType.PromptPay.Name) { return PaymentChannelType.PromptPay; }
            if (name == PaymentChannelType.VisaMasterCard_QR.Name) { return PaymentChannelType.VisaMasterCard_QR; }
            if (name == PaymentChannelType.AliPayCtoB.Name) { return PaymentChannelType.AliPayCtoB; }
            if (name == PaymentChannelType.WeChatPayCtoB.Name) { return PaymentChannelType.WeChatPayCtoB; }
            if (name == PaymentChannelType.CreditCard.Name) { return PaymentChannelType.CreditCard; }

            if (name == PaymentChannelType.ShopeePay.Name) { return PaymentChannelType.ShopeePay; }
            if (name == PaymentChannelType.LinePayCtoB.Name) { return PaymentChannelType.LinePayCtoB; }
            if (name == PaymentChannelType.PromptPayFlatRate.Name) { return PaymentChannelType.PromptPayFlatRate; }
            if (name == PaymentChannelType.LinePayBtoC.Name) { return PaymentChannelType.LinePayBtoC; }
            if (name == PaymentChannelType.TrueMoneyCtoBOffline.Name) { return PaymentChannelType.TrueMoneyCtoBOffline; }

            if (name == PaymentChannelType.TrueMoneyBtoC.Name) { return PaymentChannelType.TrueMoneyBtoC; }
            if (name == PaymentChannelType.TrueMoneyCtoBOnline.Name) { return PaymentChannelType.TrueMoneyCtoBOnline; }
            if (name == PaymentChannelType.AliPayBtoC.Name) { return PaymentChannelType.AliPayBtoC; }
            if (name == PaymentChannelType.WeChatPayBtoC.Name) { return PaymentChannelType.WeChatPayBtoC; }
            throw new Exception("Cannot ConvertFromName");
        }
    }
}
