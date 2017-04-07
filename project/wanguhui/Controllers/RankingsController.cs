using System.Collections.Generic;
using System.Web.Http;

namespace MvcApplication3.Controllers
{
    using Service;

    public class RankingsController : ApiController
    {
        /// <summary>
        /// 初始化服务类
        /// </summary>
        public Service Service = new Service();

        /// <summary>
        /// 获取前100玩家信息
        /// </summary>
        /// <returns></returns>
        public List<User> GetRankings()
        {
            return Service.GetRankings();
        }
    }
}
