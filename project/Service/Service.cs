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
        /// 所有玩家信息
        /// </summary>
        private static List<User> allUser;

        /// <summary>
        /// 存储匹配战斗信息(分出胜负)
        /// </summary>
        private static Dictionary<User, User> fightMatches;

        /// <summary>
        /// 存储平局玩家信息
        /// </summary>
        private static Dictionary<User, User> pjMatches;

        static Service()
        {
            // 获取所有玩家信息
            allUser = MySQLHelper.ExecuteQuery<User>("Select id,score from wanguhui.user");

            fightMatches = new Dictionary<User, User>();
            pjMatches = new Dictionary<User, User>();
        }

        public Service()
        {
            Task.Reset += Task_Reset;
        }

        /// <summary>
        /// 重置所有玩家信息
        /// </summary>
        private void Task_Reset()
        {
            allUser.ForEach(a => a.Score = 200);
        }

        /// <summary>
        /// 匹配用户
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <returns></returns>
        public void Match(String userId)
        {
            User matchUser;

            // 查询相应用户
            var user = allUser.Find(a => a.Id.ToString() == userId);
            user.isMatching = true;

            while (true)
            {
                // 
                if (fightMatches.ContainsKey(user) || fightMatches.ContainsValue(user) || pjMatches.ContainsKey(user) || pjMatches.ContainsValue(user))
                {
                    if (fightMatches.ContainsKey(user) || fightMatches.ContainsValue(user))
                        return fightMatches.ContainsKey(user) ? fightMatches[user] : fightMatches.Where(v => v.Value == user).Select(f => f.Key).FirstOrDefault();
                    else
                        return pjMatches.ContainsKey(user) ? pjMatches[user] : pjMatches.Where(v => v.Value == user).Select(f => f.Key).FirstOrDefault();
                }

                // asdasdasd
                matchUser = allUser.FirstOrDefault(a => Condition(user, a));
                if (matchUser == null || matchUser == user)
                {
                    Thread.Sleep(100);
                    continue;
                }

                // asdasd
                if ((!fightMatches.ContainsKey(user) && !fightMatches.ContainsValue(user)) &&
                    (!pjMatches.ContainsKey(user) && !pjMatches.ContainsValue(user)) &&
                    Condition(user, matchUser))
                {
                    lock (user)
                    {
                        lock (matchUser)
                        {
                            if ((!fightMatches.ContainsKey(user) && !fightMatches.ContainsValue(user)) && (!pjMatches.ContainsKey(user) &&
                                !pjMatches.ContainsValue(user)) && Condition(user, matchUser))
                            {
                                Fight(user, matchUser);

                                break;
                            }
                            else
                            {
                                if (fightMatches.ContainsKey(user) || fightMatches.ContainsValue(user))
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // asdasd
            if (fightMatches.ContainsKey(user) || fightMatches.ContainsValue(user))
            {
                matchUser = fightMatches.ContainsKey(user) ? fightMatches[user] : fightMatches.Where(v => v.Value == user).Select(f => f.Key).FirstOrDefault();
            }
            else
            {
                matchUser = pjMatches.ContainsKey(user) ? pjMatches[user] : pjMatches.Where(v => v.Value == user).Select(f => f.Key).FirstOrDefault();
            }

            return matchUser;
        }

        /// <summary>
        /// 是否满足匹配条件
        /// </summary>
        /// <param name="user1"></param>
        /// <param name="user2"></param>
        /// <returns>匹配成功与否</returns>
        private Boolean Condition(User user1, User user2)
        {
            return user2.isMatching == true && (user1.Score + 100 >= user2.Score && user1.Score - 100 <= user2.Score) &&
                 (!fightMatches.ContainsKey(user2) && !fightMatches.ContainsValue(user2)) &&
                (!pjMatches.ContainsKey(user2) && !pjMatches.ContainsValue(user2));
        }

        /// <summary>
        /// 获取战斗结果
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <returns>-1表示user2获胜,0表示平局,1表示user1获胜</returns>
        public Int32 GetFightResult(String userId)
        {
            var result = 0;
            var user = allUser.Find(a => a.Id.ToString() == userId);
            User otherUser = null;

            if (pjMatches.ContainsKey(user))
            {
                result = 0;
                otherUser = pjMatches[user];
            }
            else
            {
                if (pjMatches.ContainsValue(user))
                {
                    result = 0;
                    otherUser = pjMatches.Where(v => v.Value == user).Select(f => f.Key).FirstOrDefault();
                }
            }

            if (fightMatches.ContainsKey(user))
            {
                result = 1;
                otherUser = fightMatches[user];
            }
            else
            {
                if (fightMatches.ContainsValue(user))
                {
                    result = -1;
                    otherUser = fightMatches.Where(v => v.Value == user).Select(f => f.Key).FirstOrDefault();
                }
            }

            if (otherUser != null)
            {
                lock (user)
                {
                    lock (otherUser)
                    {
                        switch (result)
                        {
                            case 1:
                                if (!otherUser.isMatching)
                                {
                                    fightMatches.Remove(user);
                                }

                                break;
                            case 0:
                                if (!otherUser.isMatching)
                                {
                                    if (pjMatches.ContainsKey(otherUser))
                                        pjMatches.Remove(otherUser);
                                    else
                                        pjMatches.Remove(user);
                                }

                                break;
                            case -1:
                                if (!otherUser.isMatching)
                                {
                                    fightMatches.Remove(otherUser);
                                }

                                break;
                        }
                        user.isMatching = false;
                    }
                }
            }
            else
            {
                user.isMatching = false;
            }

            return result;
        }

        /// <summary>
        /// 获取积分前100名的玩家
        /// </summary>
        /// <param name="allUser">所有玩家</param>
        /// <returns>积分前100名的玩家</returns>
        public List<User> GetRankings()
        {
            return allUser.OrderByDescending(a => a.Score).Take(100).ToList();
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

        /// <summary>
        /// 随机产生胜负并更新信息
        /// </summary>
        /// <param name="user1"></param>
        /// <param name="user2"></param>
        private void Fight(User user1, User user2)
        {
            // 随机生成战斗结果
            var random = new Random();
            var result = random.Next(-1, 1);
            switch (result)
            {
                case -1:
                    user1.Score -= 10;
                    user2.Score += 10;

                    Update(user1);
                    Update(user2);

                    // 胜方作为Key存放
                    fightMatches.Add(user2, user1);
                    break;
                case 0:
                    pjMatches.Add(user1, user2);
                    break;
                case 1:
                    user1.Score += 10;
                    user2.Score -= 10;

                    // asdasd
                    Update(user1);
                    Update(user2);

                    // 胜方作为Key存放
                    fightMatches.Add(user1, user2);

                    break;
            }
        }
    }
}
