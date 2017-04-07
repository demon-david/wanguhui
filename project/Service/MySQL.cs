namespace Service
{
    #region using directives

    using System;
    using System.Collections.Generic;
    using MySql.Data.MySqlClient;
    using MySql.Data;
    using System.Data;
    using System.Linq;
    using System.Reflection;

    #endregion

    internal class MySQL
    {
        private static String connectionString = "server=localhost;user id=root;password=123456;persistsecurityinfo=True;database=wanguhui";

        /// <summary>
        /// 每周的积分清零
        /// </summary>
        public static void Reset()
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                var sql = @"SET SQL_SAFE_UPDATES = 0;update wanguhui.user set score=200;SET SQL_SAFE_UPDATES = 1;";
                var mySqlCommand = new MySqlCommand(sql, conn);
                mySqlCommand.ExecuteNonQuery();
            }

        }

        /// <summary>
        /// 获取所有用户信息
        /// </summary>
        /// <returns>返回所有用户信息</returns>
        public static List<User> GetAllUser()
        {
            var result = new List<User>();
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                var sql = "select id,score from wanguhui.user";
                var adapter = new MySqlDataAdapter(sql, conn);
                var ds = new DataSet();
                adapter.Fill(ds);
                result = Mapping(ds);
            }

            return result;
        }

        /// <summary>
        /// 将DataSet对象映射为相应的类对象
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private static List<User> Mapping(DataSet ds)
        {
            var result = new List<User>();
            var type = typeof(User);
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                var user = new User();
                foreach (DataColumn column in ds.Tables[0].Columns)
                {
                    type.GetProperties().ToList().ForEach(property =>
                    {
                        if (String.Equals(property.Name, column.ColumnName, StringComparison.CurrentCultureIgnoreCase))
                            property.SetValue(user, row[column], null);
                    });
                }
                result.Add(user);
            }
            return result;
        }

        /// <summary>
        /// 更新相应用户信息
        /// </summary>
        /// <param name="id">标识用户的id</param>
        /// <returns>更新成功结果</returns>
        public static Boolean Update(User user)
        {
            var result = false;
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        var sql = String.Format("update wanguhui.user set score = {0} where id = '{1}'", user.Score, user.Id);
                        var mySqlCommand = new MySqlCommand(sql, conn, tran);
                        mySqlCommand.ExecuteNonQuery();
                        tran.Commit();
                        result = true;
                    }
                    catch
                    {
                        tran.Rollback();
                    }
                }
            }

            return result;
        }

        public static void Add()
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                var mySqlCommand = new MySqlCommand(String.Format("insert into wanguhui.user(id,score) values ('{0}',{1})", Guid.NewGuid(), 200), conn);
                mySqlCommand.ExecuteNonQuery();
            }
        }
    }
}
