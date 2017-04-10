using System;
using System.Web.Http;

namespace MvcApplication3.Controllers
{
    using Service;

    /// <summary>
    /// 获取结果接口
    /// </summary>
    public class ResultController : ApiController
    {
        /// <summary>
        /// 初始化服务类
        /// </summary>
        private UserService service = new UserService();

        /// <summary>
        /// 获取战斗结果
        /// </summary>
        /// <param name="id">用户id</param>
        /// <returns>返回战斗结果</returns>
        public FightResult GetResult(String id)
        {
            return service.GetFightResult(id);
        }
    }
}
