using System;
using System.Collections.Generic;
using System.Linq;

namespace MySqlDatabase
{
    /// <summary>
    /// 与数据库交互操作类
    /// </summary>
    public class DbOperation
    {
        /// <summary>
        /// 获取指定用户
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="userId"></param>
        /// <returns></returns>
        public T GetUser<T>(String userId) where T : class,new()
        {
            var sql = String.Format("select id,score from wanguhui.user where id = '{0}'", userId);
            return MySqlHelper.ExecuteQuery<T>(sql).FirstOrDefault();
        }

        /// <summary>
        /// 获取积分排行前面的用户
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="num">前面多少个用户</param>
        /// <returns></returns>
        public List<T> GetTop<T>(Int32 num) where T : class,new()
        {
            var sql = String.Format("select id,score from wanguhui.user order by score desc limit {0}", num);
            return MySqlHelper.ExecuteQuery<T>(sql);
        }

        /// <summary>
        /// 更新用户积分信息
        /// </summary>
        /// <param name="userId">用户唯一标识</param>
        /// <param name="score">用户积分</param>
        /// <returns></returns>
        public Boolean Updata(String userId, Int32 score)
        {
            var sql = String.Format("update wanguhui.user set score = {0} where id = '{1}'", score, userId);
            return MySqlHelper.ExecuteNonQuery(sql) > 0;
        }

        /// <summary>
        /// 将所有用户信息重置
        /// </summary>
        /// <returns></returns>
        public Boolean ResetUsers()
        {
            var sql = @"SET SQL_SAFE_UPDATES = 0;update wanguhui.user set score=200;SET SQL_SAFE_UPDATES = 1;";
            return MySqlHelper.ExecuteNonQuery(sql) > 0;
        }
    }
}
