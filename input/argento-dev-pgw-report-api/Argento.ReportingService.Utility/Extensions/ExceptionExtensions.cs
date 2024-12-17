using System;
using System.Text;
using System.Text.RegularExpressions;
using Argento.ReportingService.Utility.Utils;

namespace Argento.ReportingService.Utility.Extensions
{
    public static class ExceptionExtensions
    {
        public static string GetErrorPlace(this Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            var regex = new Regex(@"at (?<errorPlace>.*) in");
            Match match = regex.Match(exception.StackTrace);
            string errorPlace = match.Groups["errorPlace"].Value;
            return errorPlace;
        }

        public static Exception GetException(this Exception exception)
        {
            Exception result = null;
            if (exception is AggregateException)
            {
                var ae = exception as AggregateException;
                ae.Handle(ex =>
                {
                    if (ex is AggregateException)
                    {
                        result = GetException(ex);
                    }
                    else
                    {
                        result = ex;
                    }
                    return true;
                });
            }
            else
            {
                result = exception;
            }

            return result;
        }

        public static string ToErrorLogs(this Exception exception)
        {
            Exception exceptionObj = ExceptionUtil.GetRealException(exception);
            string exceptionTypeName = exceptionObj.GetType().FullName;
            string innerException = (exceptionObj.InnerException == null)
                ? string.Empty : exceptionObj.InnerException.GetType().FullName;
            string exceptionMessage = exceptionObj.Message;
            string innerExceptionMessage = (exceptionObj.InnerException == null) ? string.Empty : exceptionObj.InnerException.Message;
            string exceptionStackTrace = exceptionObj.StackTrace;
            string innerExceptionStackTrace = (exceptionObj.InnerException == null) ? string.Empty : exceptionObj.InnerException.StackTrace;
            var sb = new StringBuilder();
            sb.AppendLine("[EXCEPTION] " + exceptionTypeName);
            sb.AppendLine("[EXCEPTION_MESSAGE] " + exceptionMessage);
            sb.AppendLine("[EXCEPTION_STACKTRACE] " + exceptionStackTrace);
            sb.AppendLine("[INNER_EXCEPTION] " + innerException);
            sb.AppendLine("[INNER_EXCEPTION_MESSAGE] " + innerExceptionMessage);
            sb.AppendLine("[INNER_EXCEPTION_STACKTRACE] " + innerExceptionStackTrace);

            return sb.ToString();
        }

    }
}