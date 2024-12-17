using System;

namespace Argento.ReportingService.Utility.Utils
{
    /// <summary>
    /// Library ที่เกี่ยวข้องกับ DateTime
    /// </summary>
    public static class ArcadiaDateTimeUtil
    {
        /// <summary>
        /// ดึงวันแรกของอาทิตย์นั้นๆ
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="firstDayOfWeek"></param>
        /// <returns></returns>
        public static DateTime GetFirstDateOfWeek(DateTime dateTime, DayOfWeek firstDayOfWeek)
        {
            DayOfWeek day = dateTime.DayOfWeek;
            if (day == firstDayOfWeek)
            {
                return dateTime.Date;
            }

            return GetFirstDateOfWeek(dateTime.AddDays(-1), firstDayOfWeek);
        }
    }
}