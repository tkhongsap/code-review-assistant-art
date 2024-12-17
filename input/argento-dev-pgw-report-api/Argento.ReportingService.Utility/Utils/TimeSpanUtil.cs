using System;

namespace Argento.ReportingService.Utility.Utils
{
    /// <summary>
    /// TimeSpanUtil
    /// Author : Peeradis S.
    /// Date 18/5/2018
    ///
    /// Convert String such as 10d , 36h, 50m, 20s To TimeSpan
    /// Example : ConvertToTimeSpan("99d")
    /// </summary>
    public static class TimeSpanUtil
    {
        /// <summary>
        /// แปลง string ให้กลายเป็น TimeSpan
        /// </summary>
        /// <param name="timeString"></param>
        /// <returns></returns>
        public static TimeSpan ConvertToTimeSpan(string timeString)
        {
            if (timeString != "")
            {
                int l = timeString.Length - 1;
                string value = timeString.Substring(0, 1);
                string type = timeString.Substring(l, 1);

                switch (type)
                {
                    case "d": return TimeSpan.FromDays(double.Parse(value));
                    case "h": return TimeSpan.FromHours(double.Parse(value));
                    case "m": return TimeSpan.FromMinutes(double.Parse(value));
                    case "s": return TimeSpan.FromSeconds(double.Parse(value));
                    default: return TimeSpan.FromDays(double.Parse(timeString));
                }
            }
            return TimeSpan.Zero;

        }
    }
}