using System;
using System.Text.RegularExpressions;

namespace Argento.ReportingService.Utility.Utils
{
    public static class ExceptionUtil
    {
        public static string GetErrorPlace(string stackTrace)
        {
            var regex = new Regex(@"at (?<errorPlace>.*) in");
            Match match = regex.Match(stackTrace);
            string errorPlace = match.Groups["errorPlace"].Value;
            return errorPlace;
        }

        public static Exception GetRealException(Exception exception)
        {
            Exception result = null;
            if (exception is AggregateException)
            {
                var ae = exception as AggregateException;
                ae.Handle(ex =>
                {
                    if (ex is AggregateException)
                    {
                        result = GetRealException(ex);
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

    }
}