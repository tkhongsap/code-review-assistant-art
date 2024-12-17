using System;
using System.Net;

namespace Argento.ReportingService.Utility.Exceptions
{
    public class ArcadiaException : Exception
    {

        public HttpStatusCode StatusCode { get; set; }
        public string ErrorCode { get; set; }

        public ArcadiaException(string errorCode)
            : this(errorCode, string.Empty, null, HttpStatusCode.BadRequest)
        {
        }

        public ArcadiaException(string errorCode, string message)
            : this(errorCode, message, null, HttpStatusCode.BadRequest)
        {
        }

        public ArcadiaException(string errorCode, Exception innerException)
            : this(errorCode, string.Empty, innerException, HttpStatusCode.BadRequest)
        {
        }

        public ArcadiaException(string errorCode, string message, HttpStatusCode statusCode)
            : this(errorCode, message, null, statusCode)
        {
        }

        public ArcadiaException(string errorCode, Exception innerException, HttpStatusCode statusCode)
            : this(errorCode, string.Empty, innerException, statusCode)
        {
        }

        public ArcadiaException(string errorCode, string message, Exception innerException, HttpStatusCode statusCode) : base(message, innerException)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }

    }

    public class ArcadiaBusinessFlowException : InvalidOperationException
    {
        public string ExceptionLevel { get; set; } = ArcadiaConstants.ExceptionLevel.Warning;
        public string Action { get; set; } = null;
        public object ErrorObject { get; set; } = null;
        public ArcadiaBusinessFlowException()
        {
        }

        public ArcadiaBusinessFlowException(string message) : base(message)
        {
        }


        private ArcadiaBusinessFlowException(string message, string exceptionLevel, string action, string trace) : this(message)
        {
            Action = action;
            ExceptionLevel = exceptionLevel;
        }

        public ArcadiaBusinessFlowException(object errorDetails, string exceptionLevel = ArcadiaConstants.ExceptionLevel.Warning, string action = null)
        {
            if (errorDetails is string)
            {
                throw new ArcadiaBusinessFlowException((string)errorDetails, exceptionLevel, action, this.StackTrace);
            }
            else
            {
                ErrorObject = errorDetails;
                Action = action;
                ExceptionLevel = exceptionLevel;
            }
        }
    }
}
