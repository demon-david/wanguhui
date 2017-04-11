using System;
using System.Collections.Generic;
using System.Linq;

namespace MySqlDatabase
{
    using Utils;

    /// <summary>
    /// 与用户表交互操作
    /// </summary>
    public class UserDAL
    {
        /// <summary>
        /// 获取指定用户
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="userId">用户唯一标识</param>
        /// <returns>查询用户</returns>
        public T GetUser<T>(String userId) where T : class,new()
        {
            var sql = "select id,score from wanguhui.user where id = @id";
            var paramDic = new Dictionary<String, String>
            {
                {"@id",userId}
            };

            return MySqlHelper.ExecuteQuery<T>(sql, paramDic).FirstOrDefault();
        }

        /// <summary>
        /// 获取积分排行前面多少名的用户
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="num">前面多少名用户</param>
        /// <returns>查询用户</returns>
        public List<T> GetTop<T>(Int32 num) where T : class,new()
        {
            var sql = "select id,score from wanguhui.user order by score desc limit @num";
            var paramDic = new Dictionary<String, String>
            {
                {"@num",num.ToString()}
            };

            return MySqlHelper.ExecuteQuery<T>(sql, paramDic);
        }

        /// <summary>
        /// 更新用户积分信息
        /// </summary>
        /// <param name="userId">用户唯一标识</param>
        /// <param name="score">用户积分</param>
        /// <returns>更新成功与否</returns>
        public Boolean Updata(String userId, Int32 score)
        {
            var sql = "update wanguhui.user set score = @score where id = @id";
            var paramDic = new Dictionary<String, String>
            {
                {"@score",score.ToString()},
                {"@id",userId}
            };

            return MySqlHelper.ExecuteNonQuery(sql, paramDic) > 0;
        }

        /// <summary>
        /// 将所有用户信息重置
        /// </summary>
        /// <returns>重置成功与否</returns>
        public Boolean ResetUsers()
        {
            var sql = @"SET SQL_SAFE_UPDATES = 0;update wanguhui.user set score=@score;SET SQL_SAFE_UPDATES = 1;";
            var paramDic = new Dictionary<String, String>
            {
                {"@score",200.ToString()}
            };

            return MySqlHelper.ExecuteNonQuery(sql, paramDic) > 0;
        }
    }
}
