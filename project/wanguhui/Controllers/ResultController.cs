using System;
using System.Web.Http;

namespace MvcApplication3.Controllers
{
    using Service;

    public class ResultController : ApiController
    {
        /// <summary>
        /// 初始化服务类
        /// </summary>
        public Service Service = new Service();

        /// <summary>
        /// 获取战斗结果
        /// </summary>
        /// <param name="id">用户id</param>
        /// <returns></returns>
        public FightResult GetResult(String id)
        {
            return Service.GetFightResult(id);
        }
    }
}
