namespace LAB2D.Tool
{
    using LAB2D;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 工作日判断工具（节假日/调休）。
    /// 纯数据查询，不依赖 Unity 生命周期。
    /// </summary>
    public static class DateTool
    {
        private static readonly HashSet<DateTime> holidays = new ()
        {
            new DateTime(2023, 1, 1),
            new DateTime(2023, 1, 22), new DateTime(2023, 1, 23), new DateTime(2023, 1, 24),
            new DateTime(2023, 4, 5),
            new DateTime(2023, 5, 1),
            new DateTime(2023, 6, 22),
            new DateTime(2023, 9, 29),
            new DateTime(2023, 10, 1), new DateTime(2023, 10, 2), new DateTime(2023, 10, 3),
        };

        private static readonly HashSet<DateTime> extraWorkdays = new ()
        {
            new DateTime(2023, 1, 28), new DateTime(2023, 1, 29),
            new DateTime(2023, 4, 23),
            new DateTime(2023, 5, 6),
            new DateTime(2023, 6, 25),
            new DateTime(2023, 10, 7), new DateTime(2023, 10, 8),
        };

        /// <summary>
        /// 判断指定日期是否为工作日（非周末、非法定假日，或为调休工作日）。
        /// </summary>
        public static bool IsWorkday(DateTime date)
        {
            return extraWorkdays.Contains(date.Date)
                || (date.DayOfWeek != DayOfWeek.Saturday
                    && date.DayOfWeek != DayOfWeek.Sunday
                    && !holidays.Contains(date.Date));
        }
    }
}
