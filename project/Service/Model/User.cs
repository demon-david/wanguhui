using System;

namespace Service
{
    /// <summary>
    /// 用户信息结构
    /// </summary>
    public class User
    {
        /// <summary>
        /// 用户唯一标识ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 积分
        /// </summary>
        public Int32 Score { get; set; }

        /// <summary>
        /// 是否正在进行匹配
        /// </summary>
        public Boolean IsMatching { get; set; }

        /// <summary>
        /// 战斗结果
        /// </summary>
        public FightResult Result { get; set; }

        /// <summary>
        /// 上次匹配时间
        /// </summary>
        public DateTime LastMatchTime { get; set; }

        /// <summary>
        /// 匹配到的对手
        /// </summary>
        public User MatchUser { get; set; }

        /// <summary>
        /// 用于该用于同步
        /// </summary>
        public Object lockObject { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public User ()
        {
            lockObject = new Object();
        }
    }
}
