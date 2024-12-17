using System;
using System.Collections.Generic;
using System.Threading;

namespace Argento.ReportingService.Utility.Utils
{
    public static class TimerUtil
    {
        /// <summary>
        /// เอาไว้ใช้กับ Windows Service เวลาที่ต้องตั้งเวลาการทำงานอะไรบางอย่าง
        /// ตัวอย่างข้อมูล WakeUpTime:
        /// [ "06:00:00", "18:00:00"]
        /// 
        /// </summary>
        /// <param name="wakeUpTime"></param>
        /// <returns></returns>
        public static Timer[] ExecuteTimers(TimerCallback timerCallback, params string[] wakeUpTime)
        {
            var wakeUpTimers = new List<Timer>();

            var wakeUpTimeSet = new HashSet<string>(wakeUpTime);
            foreach (string wakeupTimeString in wakeUpTimeSet)
            {
                DateTime convertedTime = Convert.ToDateTime(wakeupTimeString);

                var wakeUpDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                    convertedTime.Hour, convertedTime.Minute, convertedTime.Second);
                if (DateTime.Now > wakeUpDateTime) wakeUpDateTime = wakeUpDateTime.AddDays(1);

                // Start timer
                var wakeUpTimer = new Timer(timerCallback, null, wakeUpDateTime - DateTime.Now, TimeSpan.FromHours(24));
                wakeUpTimers.Add(wakeUpTimer);
            }

            return wakeUpTimers.ToArray();
        }
    }
}