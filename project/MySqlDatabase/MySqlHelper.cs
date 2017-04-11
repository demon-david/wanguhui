using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;

namespace MySqlDatabase
{
    /// <summary>
    /// 与数据库交互帮助类
    /// </summary>
    internal class MySqlHelper
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        private static readonly String ConnectionString = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

        /// <summary>
        /// 执行查询语句
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="sql">查询语句</param>
        /// <param name="param">查询参数</param>
        /// <returns>查询结果</returns>
        public static List<T> ExecuteQuery<T>(String sql, Dictionary<String, String> param) where T : class,new()
        {
            List<T> result;

            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                var command = new MySqlCommand(sql, conn);
                foreach (var para in param)
                {
                    command.Parameters.AddWithValue(para.Key, para.Value);
                }
                var adapter = new MySqlDataAdapter(command);
                var ds = new DataSet();
                adapter.Fill(ds);
                result = Mapping<T>(ds);
            }

            return result;
        }

        /// <summary>
        /// 执行非查询语句
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <param name="param">查询参数</param>
        /// <returns>返回受影响行数</returns>
        public static Int32 ExecuteNonQuery(String sql, Dictionary<String, String> param)
        {
            var result = 0;

            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();

                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        var mySqlCommand = new MySqlCommand(sql, conn, tran);
                        foreach (var para in param)
                        {
                            mySqlCommand.Parameters.AddWithValue(para.Key, para.Value);
                        }
                        result = mySqlCommand.ExecuteNonQuery();
                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///  将DataSet对象映射为相应的类对象
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="ds">数据集</param>
        /// <returns>类型对象列表</returns>
        private static List<T> Mapping<T>(DataSet ds) where T : new()
        {
            var result = new List<T>();

            var type = typeof(T);
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                var user = new T();

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
    }
}
