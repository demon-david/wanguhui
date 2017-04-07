
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace wanguhui.Controllers
{
    using Service;

    public class MatchController : ApiController
    {
        /// <summary>
        /// 初始化服务类
        /// </summary>
        public Service Service = new Service();

        /// <summary>
        /// 匹配对手
        /// </summary>
        /// <param name="id">用户id</param>
        /// <returns></returns>
        public void GetMatch(String id)
        {
            Service.Match(id);
        }
    }
}
