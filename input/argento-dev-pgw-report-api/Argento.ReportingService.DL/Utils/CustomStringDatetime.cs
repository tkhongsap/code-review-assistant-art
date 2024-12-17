using System;
using System.Globalization;

namespace Argento.ReportingService.DL.Utils
{
    public static class CustomStringDatetime
    {
        public static DateTime ConvertStringToDateTimeUTC(string input, string format)
        {
            try
            {
                if (DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                {
                    if (dt.Kind == DateTimeKind.Unspecified)
                    {
                        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
                        dt = TimeZoneInfo.ConvertTimeToUtc(dt, tz);

                        return dt;
                    }

                    dt = TimeZoneInfo.ConvertTimeToUtc(dt);

                    return dt;
                }

                throw new Exception($"input: {input} cannot converted.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static string ConvertDateTimeUTCtoBangkokString(DateTime? input, string format)
        {
            try
            {
                if (!input.HasValue)
                {
                    return "";
                }

                TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");

                DateTime dt = TimeZoneInfo.ConvertTimeFromUtc(input.Value, tz);

                return dt.ToString(format);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
