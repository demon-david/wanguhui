using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Service
{
    /// <summary>
    /// 战斗结果
    /// </summary>
    public enum FightResult
    {
        /// <summary>
        /// 没有战斗
        /// </summary>
        None = 0,
        /// <summary>
        /// 正在战斗还未分出胜负
        /// </summary>
        Fighting = 1,
        /// <summary>
        /// 胜利
        /// </summary>
        Win = 2,
        /// <summary>
        /// 平局
        /// </summary>
        Pj = 3,
        /// <summary>
        /// 输
        /// </summary>
        Lose = 4
    }
}
