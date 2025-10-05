namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class DateTool : MonoBehaviour
    {
        // 节假日列表
        private readonly HashSet<DateTime> holidays = new ()
        {
            new DateTime(2023, 1, 1),   // 元旦
            new DateTime(2023, 1, 22),  // 春节
            new DateTime(2023, 1, 23),
            new DateTime(2023, 1, 24),
            new DateTime(2023, 4, 5),   // 清明节
            new DateTime(2023, 5, 1),   // 劳动节
            new DateTime(2023, 6, 22),  // 端午节
            new DateTime(2023, 9, 29),  // 中秋节
            new DateTime(2023, 10, 1),  // 国庆节
            new DateTime(2023, 10, 2),
            new DateTime(2023, 10, 3),
        };

        // 调休列表
        private readonly HashSet<DateTime> extraWorkdays = new ()
        {
            new DateTime(2023, 1, 28),  // 春节调休
            new DateTime(2023, 1, 29),
            new DateTime(2023, 4, 23),  // 劳动节调休
            new DateTime(2023, 5, 6),
            new DateTime(2023, 6, 25),  // 端午节调休
            new DateTime(2023, 10, 7),  // 国庆节调休
            new DateTime(2023, 10, 8),
        };

        /// <summary>
        /// 检查是否是调休工作日
        /// 检查是否是周末
        /// 检查是否是节假日
        /// </summary>
        /// <param name="date">日期</param>
        /// <returns>是否是工作日</returns>
        public bool IsWorkday(DateTime date)
        {
            return !(this.extraWorkdays.Contains(date.Date)
                || date.DayOfWeek == DayOfWeek.Saturday
                || date.DayOfWeek == DayOfWeek.Sunday
                || this.holidays.Contains(date.Date));
        }
    }
}
