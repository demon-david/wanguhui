﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Service
{
    /// <summary>
    /// 逻辑服务类
    /// </summary>
    public class Service
    {
        /// <summary>
        /// 最近匹配过的用户信息
        /// </summary>
        private static List<User> matchUsers;

        /// <summary>
        /// 用于处理matchUser数据结构的读写锁
        /// </summary>
        private static ReaderWriterLockSlim matchRwl;

        /// <summary>
        /// 用于处理排行榜的读写锁
        /// </summary>
        private static ReaderWriterLockSlim rankingsRwl;


        private static ManualResetEvent manualResetEvent;

        /// <summary>
        /// 进行匹配的线程
        /// </summary>
        private static Thread matchThread;

        /// <summary>
        /// 从匹配队列中移除长时间没有匹配的用户
        /// </summary>
        private static Timer removeUserThread;

        static Service()
        {
            matchUsers = new List<User>();
            matchRwl = new ReaderWriterLockSlim();
            rankingsRwl = new ReaderWriterLockSlim();
            manualResetEvent = new ManualResetEvent(false);

            // 初始化排行榜
            Rankings.RankingUsers =
                MySQLHelper.ExecuteQuery<User>("select id,score from wanguhui.user order by score desc limit 200");

            // 初始化匹配线程
            matchThread = new Thread(MatchThread);
            matchThread.IsBackground = true;
            matchThread.Start();

            // 初始化移除长时间没有进行匹配的用户的Timer线程
            removeUserThread = new Timer(RemoveUserThread, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        public Service()
        {
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
                    user = MySQLHelper.ExecuteQuery<User>(String.Format("select id,score from wanguhui.user where id='{0}'", userId))[0];
                    matchUsers.Add(user);
                }
                finally
                {
                    matchRwl.ExitWriteLock();
                }
            }

            // 重置用户信息
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
                        // 该用户在匹配中且尚未找到匹配对手
                        if (user.IsMatching && user.MatchUser == null)
                        {
                            var result = Monitor.TryEnter(user);
                            if (result)
                            {
                                if (user.IsMatching && user.MatchUser == null)
                                {
                                    try
                                    {
                                        for (int j = 0; j < matchUsers.Count; j++)
                                        {
                                            // 匹配条件:用户不同,正在匹配中,积分差距不超过100
                                            if (user != matchUsers[j] && matchUsers[j].IsMatching && matchUsers[j].MatchUser == null
                                                && user.Score + 100 >= matchUsers[j].Score && user.Score - 100 <= matchUsers[j].Score)
                                            {
                                                lock (matchUsers[j])
                                                {
                                                    if (user != matchUsers[j] && matchUsers[j].IsMatching && matchUsers[j].MatchUser == null
                                                        && user.Score + 100 >= matchUsers[j].Score && user.Score - 100 <= matchUsers[j].Score)
                                                    {
                                                        // 更新用户匹配信息
                                                        user.MatchUser = matchUsers[j];
                                                        user.Result = FightResult.Fighting;
                                                        matchUsers[j].MatchUser = user;
                                                        matchUsers[j].Result = FightResult.Fighting;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        Monitor.Exit(user);
                                    }
                                }
                            }
                        }
                    }

                    matchRwl.ExitReadLock();

                    Thread.Sleep(20);
                }
            }
        }

        /// <summary>
        /// 处理匹配数组线程
        /// </summary>
        /// <param name="state"></param>
        private static void RemoveUserThread(Object state)
        {
            matchRwl.EnterWriteLock();

            try
            {
                var now = DateTime.Now;

                // 当上次匹配时间距离现在超过一个小时,将用户从匹配数组中移除
                for (int i = 0; i < matchUsers.Count; i++)
                {
                    if (now - matchUsers[i].LastMatchTime > TimeSpan.FromHours(1))
                    {
                        matchUsers.RemoveAt(i);
                        i--;
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
        /// <returns>-1表示user2获胜,0表示平局,1表示user1获胜</returns>
        public FightResult GetFightResult(String userId)
        {
            // 战斗结果
            var result = FightResult.None;

            // 匹配到的对手
            User matchUser = null;

            // 查找用户
            var user = matchUsers.Find(a => a.Id.ToString() == userId);
            if (user != null && user.MatchUser != null)
            {
                // 如果用户还未生成战斗结果,随机生成战斗结果
                if (user.Result == FightResult.Fighting)
                {
                    matchUser = user.MatchUser;
                    lock (user)
                    {
                        lock (matchUser)
                        {
                            var random = new Random();
                            switch (random.Next(-1, 2))
                            {
                                // 输
                                case -1:
                                    // 更新用户积分信息
                                    user.Score -= 10;
                                    matchUser.Score += 10;
                                    //// 更新用户数据库信息
                                    Update(user);
                                    Update(matchUser);

                                    // 更新战斗结果
                                    user.Result = FightResult.Lose;
                                    matchUser.Result = FightResult.Win;

                                    // 更新排行榜
                                    UpdateRankings(user);
                                    UpdateRankings(matchUser);

                                    break;
                                // 平局
                                case 0:
                                    // 更新战斗结果
                                    user.Result = FightResult.Pj;
                                    matchUser.Result = FightResult.Pj;

                                    break;
                                // 胜利
                                case 1:
                                    // 更新用户积分信息
                                    user.Score += 10;
                                    matchUser.Score -= 10;
                                    //// 更新用户数据库信息
                                    Update(user);
                                    Update(matchUser);

                                    // 更新战斗结果
                                    user.Result = FightResult.Win;
                                    matchUser.Result = FightResult.Lose;

                                    // 更新排行榜
                                    UpdateRankings(user);
                                    UpdateRankings(matchUser);

                                    break;
                            }
                        }
                    }
                }

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
        private void UpdateRankings(User user)
        {
            var lowestScoreInRankings = 0;

            lock (Rankings.RankingUsers)
            {
                lowestScoreInRankings = Rankings.RankingUsers[Rankings.RankingUsers.Count - 1].Score;
            }

            if (user.Score > lowestScoreInRankings)
            {
                rankingsRwl.EnterWriteLock();

                // 更新排行榜用户信息
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
                    Rankings.RankingUsers.Find(u => u.Id == user.Id).Score = user.Score;
                }

                Rankings.RankingUsers = Rankings.RankingUsers.OrderByDescending(u => u.Score).ToList();

                rankingsRwl.ExitWriteLock();
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
        /// 更新用户积分信息
        /// </summary>
        /// <param name="user"></param>
        private void Update(User user)
        {
            var sql = String.Format("update wanguhui.user set score = {0} where id = '{1}'", user.Score, user.Id);
            MySQLHelper.ExecuteNonQuery(sql);
        }
    }
}
