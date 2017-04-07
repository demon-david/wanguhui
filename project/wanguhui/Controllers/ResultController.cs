using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace MvcApplication3.Controllers
{
    public class ResultController : ApiController
    {
        public Service.Service Service = new Service.Service();

        /// <summary>
        /// 获取战斗结果
        /// </summary>
        /// <param name="id">用户id</param>
        /// <returns></returns>
        public Int32 GetResult(String id)
        {
            return Service.GetFightResult(id);
        }
    }
}
