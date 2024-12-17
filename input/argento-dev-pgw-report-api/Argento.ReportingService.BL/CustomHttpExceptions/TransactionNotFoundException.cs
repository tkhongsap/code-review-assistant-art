using System;
using System.Net;

namespace Argento.ReportingService.BL.CustomHttpExceptions
{
    public class TransactionNotFoundException : Exception, ICustomHttpException
    {
        private readonly HttpStatusCode _StatusCode = HttpStatusCode.NotFound;
        private readonly string _RespCode = "4004";
        private static readonly string _RespDesc = "transaction not found";

        public TransactionNotFoundException() : base(TransactionNotFoundException._RespDesc)
        {

        }

        public HttpStatusCode StatusCode { get => _StatusCode; }
        public string RespCode { get => _RespCode; }
        public string RespDesc { get => _RespDesc; }
    }
}
