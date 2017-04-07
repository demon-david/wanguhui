using Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace MvcApplication3.Controllers
{
    public class RankingsController : ApiController
    {
        public Service.Service Service = new Service.Service();

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
