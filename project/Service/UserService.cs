using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Service
{
    using MySqlDatabase;

    /// <summary>
    /// 逻辑服务类
    /// </summary>
    public class UserService
    {
        #region 静态字段

        /// <summary>
        /// 最近一个小时匹配过的用户
        /// </summary>
        private static readonly List<User> mMatchUsers;

        /// <summary>
        /// 用于同步最近匹配过的用户结构matchUsers
        /// </summary>
        private static readonly ReaderWriterLockSlim mMatchRwl;

        /// <summary>
        /// 用于同步排行榜
        /// </summary>
        private static readonly ReaderWriterLockSlim mRankingsRwl;

        /// <summary>
        /// 用于同步匹配线程中的匹配逻辑
        /// </summary>
        private static readonly ManualResetEvent mManualResetEvent;

        /// <summary>
        /// 从匹配数组中移除最近一个小时没有进行匹配的用户处理线程
        /// </summary>
        private static Timer mRemoveUserThread;

        /// <summary>
        /// 与数据库交互操作
        /// </summary>
        private static readonly UserDAL DbOperation = new UserDAL();

        /// <summary>
        /// 用户起始积分
        /// </summary>
        private const Int32 mStartScore = 200;
        
        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化静态字段
        /// </summary>
        static UserService()
        {
            // 字段初始化
            mMatchUsers = new List<User>();
            mMatchRwl = new ReaderWriterLockSlim();
            mRankingsRwl = new ReaderWriterLockSlim();
            mManualResetEvent = new ManualResetEvent(false);

            // 初始化排行榜
            ScoreRankings.ScoreRankingUsers = DbOperation.GetTop<User>(200);

            // 初始化匹配线程
            var matchThread = new Thread(MatchThread) { IsBackground = true };
            matchThread.Start();

            // 初始化移除最近一个小时没有进行匹配的用户的线程,一小时触发一次
            mRemoveUserThread = new Timer(RemoveUsers, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        /// <summary>
        /// 初始化实例字段
        /// </summary>
        public UserService()
        {
            // 注册重置事件
            Task.Reset += Task_Reset;
        }
        
        #endregion

        #region 用户信息重置

        /// <summary>
        /// 重置用户信息
        /// </summary>
        private static void Task_Reset()
        {
            // 重置匹配数组中用户信息
            mMatchRwl.EnterWriteLock();

            mMatchUsers.ForEach(a => a.Score = mStartScore);

            mMatchRwl.ExitWriteLock();

            // 重置排行榜中用户信息
            mRankingsRwl.EnterWriteLock();

            ScoreRankings.ScoreRankingUsers.ForEach(a => a.Score = mStartScore);

            mRankingsRwl.ExitWriteLock();
        }
        
        #endregion

        #region 匹配相关

        /// <summary>
        ///  匹配用户线程
        /// </summary>
        private static void MatchThread()
        {
            while (true)
            {
                // 如果匹配数组中的用户个数小于2,则等待
                if (mMatchUsers.Count < 2)
                {
                    mManualResetEvent.WaitOne();
                }
                else
                {
                    // 匹配逻辑处理
                    mMatchRwl.EnterReadLock();

                    // 遍历每个用户查找相应的匹配用户
                    foreach (var user in mMatchUsers.AsParallel())
                    {
                        // 用户尚未开始匹配或者用户已匹配成功,则不进行匹配
                        if (!user.IsMatching || user.MatchUser != null)
                        {
                            continue;
                        }

                        // 该用户在匹配中且尚未找到匹配对手,进行匹配
                        var result = Monitor.TryEnter(user);
                        if (result)
                        {
                            // 用户尚未开始匹配或者用户已匹配成功,则不进行匹配
                            if ((!user.IsMatching) || user.MatchUser != null)
                            {
                                continue;
                            }

                            try
                            {
                                // 查找匹配用户
                                FindMatchUser(user);
                            }
                            finally
                            {
                                Monitor.Exit(user);
                            }
                        }
                    }

                    mMatchRwl.ExitReadLock();

                    Thread.Sleep(20);
                }
            }
        }

        /// <summary>
        /// 查找匹配用户
        /// </summary>
        /// <param name="user">与该用户进行匹配</param>
        private static void FindMatchUser(User user)
        {
            // 循环遍历每个用户去查找匹配用户
            foreach (var item in mMatchUsers.AsParallel())
            {
                // 当用户在匹配中、尚未匹配成功、满足匹配条件时,进行匹配
                if (user != item && item.IsMatching && item.MatchUser == null
                    && user.Score + 100 >= item.Score && user.Score - 100 <= item.Score)
                {
                    lock (item)
                    {
                        if (user != item && item.IsMatching && item.MatchUser == null
                            && user.Score + 100 >= item.Score && user.Score - 100 <= item.Score)
                        {
                            // 更新用户匹配信息
                            user.MatchUser = item;
                            user.Result = FightResult.Fighting;
                            item.MatchUser = user;
                            item.Result = FightResult.Fighting;

                            // 随机生成战斗结果
                            GenerateFightResult(user, item);

                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 开始匹配
        /// </summary>
        /// <param name="userId">用户唯一标识ID</param>
        public void StartMatch(String userId)
        {
            User user;

            // 查询用户信息
            mMatchRwl.EnterReadLock();

            user = mMatchUsers.Find(a => a.Id.ToString() == userId);

            mMatchRwl.ExitReadLock();

            // 匹配数组不存在该用户时,从数据库中获取用户信息并存入匹配数组中
            if (user == null)
            {
                mMatchRwl.EnterWriteLock();

                try
                {
                    user = DbOperation.GetUser<User>(userId);
                    mMatchUsers.Add(user);
                }
                finally
                {
                    mMatchRwl.ExitWriteLock();
                }
            }

            // 初始化用户信息
            user.IsMatching = true;
            user.MatchUser = null;
            user.LastMatchTime = DateTime.Now;
            user.Result = FightResult.None;

            mManualResetEvent.Set();
        }

        #endregion

        #region 积分相关

        /// <summary>
        /// 生成战斗结果
        /// </summary>
        /// <param name="user1">其中一个用户</param>
        /// <param name="user2">另一个用户</param>
        private static void GenerateFightResult(User user1, User user2)
        {
            var random = new Random();

            // 随机生成战斗结果,-1表示输,0表示平局,1表示赢
            switch (random.Next(-1, 2))
            {
                case -1:// 输
                    // 更新用户积分信息
                    user1.Score -= 10;
                    user2.Score += 10;
                    //// 更新用户数据库信息
                    Update(user1);
                    Update(user2);

                    // 更新战斗结果
                    user1.Result = FightResult.Lose;
                    user2.Result = FightResult.Win;

                    // 更新排行榜
                    UpdateRankings(user1);
                    UpdateRankings(user2);

                    break;

                case 0:// 平局
                    // 更新战斗结果
                    user1.Result = FightResult.Pj;
                    user2.Result = FightResult.Pj;

                    break;

                case 1:// 赢
                    // 更新用户积分信息
                    user1.Score += 10;
                    user2.Score -= 10;
                    //// 更新用户数据库信息
                    Update(user1);
                    Update(user2);

                    // 更新战斗结果
                    user1.Result = FightResult.Win;
                    user2.Result = FightResult.Lose;

                    // 更新排行榜
                    UpdateRankings(user1);
                    UpdateRankings(user2);

                    break;
            }
        }

        /// <summary>
        /// 更新用户数据库积分
        /// </summary>
        /// <param name="user"></param>
        private static void Update(User user)
        {
            DbOperation.Updata(user.Id.ToString(), user.Score);
        }

        #endregion

        #region 获取战斗结果

        /// <summary>
        /// 获取战斗结果
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <returns>战斗结果</returns>
        public FightResult GetFightResult(String userId)
        {
            var result = FightResult.None;

            // 查找用户
            var user = mMatchUsers.Find(a => a.Id.ToString() == userId);

            // 获取战斗结果,并更新用户信息
            if (user != null && user.MatchUser != null)
            {
                result = user.Result;
                user.Result = FightResult.None;
                user.IsMatching = false;
                user.MatchUser = null;
            }

            return result;
        }

        #endregion

        #region 移除最近一个小时没有进行匹配的用户

        /// <summary>
        /// 移除最近一个小时没有进行匹配的用户
        /// </summary>
        /// <param name="state">一个与方法相关的对象或为 null</param>
        private static void RemoveUsers(Object state)
        {
            mMatchRwl.EnterWriteLock();

            try
            {
                // 获取当前时间
                var now = DateTime.Now;

                // 当上次匹配时间距离现在超过一个小时,将用户从匹配数组中移除
                foreach (var user in mMatchUsers.AsParallel())
                {
                    if (now - user.LastMatchTime > TimeSpan.FromHours(1))
                    {
                        mMatchUsers.Remove(user);
                    }
                }
            }
            finally
            {
                mMatchRwl.ExitWriteLock();
            }
        }
        
        #endregion

        #region 排行榜相关

        /// <summary>
        /// 更新排行榜
        /// </summary>
        /// <param name="user">用户</param>
        private static void UpdateRankings(User user)
        {
            // 排行榜中最低积分
            Int32 lowestScoreInRankings;

            // 获取排行榜中最低积分
            lock (ScoreRankings.ScoreRankingUsers)
            {
                lowestScoreInRankings = ScoreRankings.ScoreRankingUsers[ScoreRankings.ScoreRankingUsers.Count - 1].Score;
            }

            // 当用户积分大于排行榜中最低积分时,更新排行榜
            if (user.Score > lowestScoreInRankings)
            {
                mRankingsRwl.EnterWriteLock();

                // 更新排行榜用户信息
                try
                {
                    // 当该用户不再排行榜中时,加入排行榜中
                    if (!ScoreRankings.ScoreRankingUsers.Exists(u => u.Id == user.Id))
                    {
                        if (ScoreRankings.ScoreRankingUsers.Count == 200)
                        {
                            ScoreRankings.ScoreRankingUsers[ScoreRankings.ScoreRankingUsers.Count - 1] = user;
                        }
                        else
                        {
                            ScoreRankings.ScoreRankingUsers.Add(user);
                        }
                    }
                    else
                    {
                        // 更新排行榜中用户积分
                        ScoreRankings.ScoreRankingUsers.Find(u => u.Id == user.Id).Score = user.Score;
                    }

                    // 对排行榜进行重新排序
                    ScoreRankings.ScoreRankingUsers = ScoreRankings.ScoreRankingUsers.OrderByDescending(u => u.Score).ToList();
                }
                finally
                {
                    mRankingsRwl.ExitWriteLock();
                }
            }
            else
            {
                // 当该用户积分低于排行榜最低积分时,将其从排行榜中移除
                if (ScoreRankings.ScoreRankingUsers.Exists(u => u.Id == user.Id))
                {
                    mRankingsRwl.EnterWriteLock();

                    ScoreRankings.ScoreRankingUsers.Remove(ScoreRankings.ScoreRankingUsers.Find(u => u.Id == user.Id));

                    mRankingsRwl.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// 获取积分前100名的玩家
        /// </summary>
        /// <returns>积分前100名的玩家</returns>
        public List<User> GetRankings()
        {
            return ScoreRankings.ScoreRankingUsers.OrderByDescending(u => u.Score).Take(100).ToList();
        }

        #endregion
    }
}
