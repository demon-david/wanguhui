using System;
using System.Threading;

namespace Service
{
    using MySqlDatabase;

    /// <summary>
    /// 处理周期性任务
    /// </summary>
    internal class Task
    {
        /// <summary>
        /// 每周结算和积分清零线程
        /// </summary>
        private static Timer task;

        /// <summary>
        /// 重置事件
        /// </summary>
        public static event Action Reset;

        /// <summary>
        /// 与数据库交互类
        /// </summary>
        private static readonly DbOperation DbOperation = new DbOperation();

        static Task()
        {
            // 今天时间
            var today = DateTime.Now;

            // 计算距离每周结算日期剩余的毫秒数
            var dayLength = (7 - (Int32)today.DayOfWeek) % 7 + 1;
            var millisecondLength = GetMillisecond((TimeSpan.FromDays(dayLength))) - GetMillisecond(today - today.Date);

            // 初始化线程,第一次触发在该周周末,后面每次触发间隔一周
            task = new Timer(TimerCallback, null, millisecondLength, GetMillisecond(TimeSpan.FromDays(7)));
        }

        /// <summary>
        /// 每周末对比赛进行清算和积分清零
        /// </summary>
        /// <param name="state"></param>
        private static void TimerCallback(Object state)
        {
            // 将所有玩家积分重置
            var result = DbOperation.ResetUsers();

            // 更新用户数据库信息后,触发重置事件
            if (result)
            {
                if (Reset != null)
                {
                    Reset();
                }
            }
        }

        /// <summary>
        /// 计算毫秒
        /// </summary>
        /// <param name="time"></param>
        /// <returns>返回TimeSpan的毫秒数</returns>
        private static Int32 GetMillisecond(TimeSpan time)
        {
            return (((time.Days * 24 + time.Hours) * 60 + time.Minutes) * 60 + time.Seconds) * 1000 + time.Milliseconds;
        }
    }
}
