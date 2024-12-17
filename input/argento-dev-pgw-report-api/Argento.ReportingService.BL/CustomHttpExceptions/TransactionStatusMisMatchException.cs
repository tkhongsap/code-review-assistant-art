using System;
using System.Net;

namespace Argento.ReportingService.BL.CustomHttpExceptions
{
    public class TransactionStatusMisMatchException : Exception, ICustomHttpException
    {
        private readonly HttpStatusCode _StatusCode = HttpStatusCode.UnprocessableEntity;
        private readonly string _RespCode = "4006";
        private static readonly string _RespDesc = "There are some transaction status mismatch";

        public TransactionStatusMisMatchException() : base(TransactionStatusMisMatchException._RespDesc)
        {

        }
        public HttpStatusCode StatusCode { get => _StatusCode; }
        public string RespCode { get => _RespCode; }
        public string RespDesc { get => _RespDesc; }
    }
}
