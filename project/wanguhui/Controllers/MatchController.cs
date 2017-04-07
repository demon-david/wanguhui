using Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace wanguhui.Controllers
{
    public class MatchController : ApiController
    {
        public Service.Service Service = new Service.Service();

        /// <summary>
        /// 匹配对手
        /// </summary>
        /// <param name="id">用户id</param>
        /// <returns></returns>
        public User GetMatch(String id)
        {
            return Service.Match(id);
            //return new User { Id = Guid.NewGuid(), Score = 250 };
        }
    }
}
