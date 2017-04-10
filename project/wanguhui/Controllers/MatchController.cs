using System;
using System.Web.Http;

namespace wanguhui.Controllers
{
    using Service;

    /// <summary>
    /// 发起匹配接口
    /// </summary>
    public class MatchController : ApiController
    {
        /// <summary>
        /// 初始化服务类
        /// </summary>
        private UserService service = new UserService();

        /// <summary>
        /// 匹配对手
        /// </summary>
        /// <param name="id">用户id</param>
        /// <returns>进入匹配成功与否</returns>
        public Boolean GetMatch(String id)
        {
            var result = false;
            try
            {
                service.StartMatch(id);
                result = true;
            }
            catch
            {
            }
            return result;
        }
    }
}
