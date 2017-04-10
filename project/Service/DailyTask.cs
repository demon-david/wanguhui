using System;
using System.Threading;

namespace Service
{
    using MySqlDatabase;

    /// <summary>
    /// 处理周期性任务
    /// </summary>
    internal class DailyTask
    {
        /// <summary>
        /// 每周结算和积分清零线程
        /// </summary>
        private static Timer mTask;

        /// <summary>
        /// 与数据库交互类
        /// </summary>
        private static readonly UserDAL mDbOperation = new UserDAL();

        /// <summary>
        /// 重置事件
        /// </summary>
        public static event Action Reset;

        /// <summary>
        /// 初始化静态字段
        /// </summary>
        static DailyTask()
        {
            // 今天时间
            var today = DateTime.Now;

            // 计算距离每周结算日期剩余的毫秒数
            var dayLength = (7 - (Int32)today.DayOfWeek) % 7 + 1;
            var millisecondLength = (Int32)(TimeSpan.FromDays(dayLength).TotalMilliseconds - (today - today.Date).TotalMilliseconds);

            // 初始化线程,第一次触发在该周周末,后面每次触发间隔一周
            mTask = new Timer(TimerCallback, null, millisecondLength, (Int32)TimeSpan.FromDays(7).TotalMilliseconds);
        }

        /// <summary>
        /// 每周末对比赛进行清算和积分清零
        /// </summary>
        /// <param name="state"></param>
        private static void TimerCallback(Object state)
        {
            // 将所有玩家积分重置
            var result = mDbOperation.ResetUsers();

            // 更新用户数据库信息后,触发重置事件
            if (result)
            {
                if (Reset != null)
                {
                    Reset();
                }
            }
        }
    }
}
