using System.Collections.Generic;
using System.Web.Http;

namespace MvcApplication3.Controllers
{
    using Service;

    /// <summary>
    /// 获取排行榜接口
    /// </summary>
    public class RankingsController : ApiController
    {
        /// <summary>
        /// 初始化服务类
        /// </summary>
        private UserService service = new UserService();

        /// <summary>
        /// 获取前100用户信息
        /// </summary>
        /// <returns>前100名用户信息</returns>
        public List<User> GetRankings()
        {
            return service.GetRankings();
        }
    }
}
