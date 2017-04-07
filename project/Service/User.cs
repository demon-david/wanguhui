using System;

namespace Service
{
    /// <summary>
    /// 用户信息结构
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }

        public Int32 Score { get; set; }

        public Boolean isMatching { get; set; }
    }
}
