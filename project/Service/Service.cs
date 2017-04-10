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
    public class Service
    {
        /// <summary>
        /// 最近一个小时匹配过的用户
        /// </summary>
        private static List<User> matchUsers;

        /// <summary>
        /// 同步最近匹配过的用户结构matchUsers
        /// </summary>
        private static ReaderWriterLockSlim matchRwl;

        /// <summary>
        /// 同步排行榜
        /// </summary>
        private static ReaderWriterLockSlim rankingsRwl;

        /// <summary>
        /// 同步匹配线程中的匹配逻辑
        /// </summary>
        private static ManualResetEvent manualResetEvent;

        /// <summary>
        /// 对用户按照一定规则进行匹配
        /// </summary>
        private static Thread matchThread;

        /// <summary>
        /// 从匹配数组中移除最近一个小时没有进行匹配的用户
        /// </summary>
        private static Timer removeUserThread;

        /// <summary>
        /// 与数据库交互的操作
        /// </summary>
        private static DbOperation dbOperation = new DbOperation();

        static Service()
        {
            // 字段初始化
            matchUsers = new List<User>();
            matchRwl = new ReaderWriterLockSlim();
            rankingsRwl = new ReaderWriterLockSlim();
            manualResetEvent = new ManualResetEvent(false);

            // 初始化排行榜
            Rankings.RankingUsers = dbOperation.GetTop<User>(200);

            // 初始化匹配线程
            matchThread = new Thread(MatchThread);
            matchThread.IsBackground = true;
            matchThread.Start();

            // 初始化移除最近一个小时没有进行匹配的用户的线程
            removeUserThread = new Timer(RemoveUserThread, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        public Service()
        {
            // 注册重置事件
            Task.Reset += Task_Reset;
        }

        /// <summary>
        /// 重置用户信息
        /// </summary>
        private void Task_Reset()
        {
            // 重置匹配数组中用户信息
            matchRwl.EnterWriteLock();

            matchUsers.ForEach(a => a.Score = 200);

            matchRwl.ExitWriteLock();

            // 重置排行榜中用户信息
            rankingsRwl.EnterWriteLock();

            Rankings.RankingUsers.ForEach(a => a.Score = 200);

            rankingsRwl.ExitWriteLock();
        }

        /// <summary>
        /// 开始匹配
        /// </summary>
        /// <param name="userId">用户唯一标识ID</param>
        public void StartMatch(String userId)
        {
            User user;

            // 查询用户信息
            matchRwl.EnterReadLock();

            user = matchUsers.Find(a => a.Id.ToString() == userId);

            matchRwl.ExitReadLock();

            // 匹配数组不存在该用户时,从数据库中获取用户信息并存入匹配数组中
            if (user == null)
            {
                matchRwl.EnterWriteLock();

                try
                {
                    user = dbOperation.GetUser<User>(userId);
                    matchUsers.Add(user);
                }
                finally
                {
                    matchRwl.ExitWriteLock();
                }
            }

            // 初始化用户信息
            user.IsMatching = true;
            user.MatchUser = null;
            user.LastMatchTime = DateTime.Now;
            user.Result = FightResult.None;

            manualResetEvent.Set();
        }

        /// <summary>
        ///  匹配用户线程
        /// </summary>
        private static void MatchThread()
        {
            while (true)
            {
                // 如果匹配数组中的用户个数小于2,则等待
                if (matchUsers.Count < 2)
                {
                    manualResetEvent.WaitOne();
                }
                else
                {
                    // 匹配逻辑处理
                    matchRwl.EnterReadLock();

                    // 遍历每个用户查找相应的匹配用户
                    foreach (var user in matchUsers.AsParallel())
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

                    matchRwl.ExitReadLock();

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
            foreach (var item in matchUsers.AsParallel())
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
        /// 生成战斗结果
        /// </summary>
        /// <param name="user1"></param>
        /// <param name="user2"></param>
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
        /// 移除最近一个小时没有进行匹配的用户
        /// </summary>
        /// <param name="state"></param>
        private static void RemoveUserThread(Object state)
        {
            matchRwl.EnterWriteLock();

            try
            {
                // 获取当前时间
                var now = DateTime.Now;

                // 当上次匹配时间距离现在超过一个小时,将用户从匹配数组中移除
                foreach (var user in matchUsers.AsParallel())
                {
                    if (now - user.LastMatchTime > TimeSpan.FromHours(1))
                    {
                        matchUsers.Remove(user);
                    }
                }
            }
            finally
            {
                matchRwl.ExitWriteLock();
            }
        }

        /// <summary>
        /// 获取战斗结果
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <returns></returns>
        public FightResult GetFightResult(String userId)
        {
            var result = FightResult.None;

            // 查找用户
            var user = matchUsers.Find(a => a.Id.ToString() == userId);

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

        /// <summary>
        /// 更新排行榜
        /// </summary>
        /// <param name="user"></param>
        private static void UpdateRankings(User user)
        {
            // 排行榜中最低积分
            var lowestScoreInRankings = 0;

            // 获取排行榜中最低积分
            lock (Rankings.RankingUsers)
            {
                lowestScoreInRankings = Rankings.RankingUsers[Rankings.RankingUsers.Count - 1].Score;
            }

            // 当用户积分大于排行榜中最低积分时,更新排行榜
            if (user.Score > lowestScoreInRankings)
            {
                rankingsRwl.EnterWriteLock();

                // 更新排行榜用户信息
                try
                {
                    // 当该用户不再排行榜中时,加入排行榜中
                    if (!Rankings.RankingUsers.Exists(u => u.Id == user.Id))
                    {
                        if (Rankings.RankingUsers.Count == 200)
                        {
                            Rankings.RankingUsers[Rankings.RankingUsers.Count - 1] = user;
                        }
                        else
                        {
                            Rankings.RankingUsers.Add(user);
                        }
                    }
                    else
                    {
                        // 更新排行榜中用户积分
                        Rankings.RankingUsers.Find(u => u.Id == user.Id).Score = user.Score;
                    }

                    // 对排行榜进行重新排序
                    Rankings.RankingUsers = Rankings.RankingUsers.OrderByDescending(u => u.Score).ToList();
                }
                finally
                {
                    rankingsRwl.ExitWriteLock();
                }
            }
            else
            {
                // 当该用户积分低于排行榜最低积分时,将其从排行榜中移除
                if (Rankings.RankingUsers.Exists(u => u.Id == user.Id))
                {
                    rankingsRwl.EnterWriteLock();

                    Rankings.RankingUsers.Remove(Rankings.RankingUsers.Find(u => u.Id == user.Id));

                    rankingsRwl.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// 获取积分前100名的玩家
        /// </summary>
        /// <returns>积分前100名的玩家</returns>
        public List<User> GetRankings()
        {
            return Rankings.RankingUsers.OrderByDescending(u => u.Score).Take(100).ToList();
        }

        /// <summary>
        /// 更新用户数据库积分
        /// </summary>
        /// <param name="user"></param>
        private static void Update(User user)
        {
            dbOperation.Updata(user.Id.ToString(), user.Score);
        }
    }
}
