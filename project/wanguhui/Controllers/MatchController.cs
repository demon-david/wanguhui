using System;
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
        public Boolean GetMatch(String id)
        {
            var result = false;
            try
            {
                Service.StartMatch(id);
                result = true;
            }
            catch
            {
            }
            return result;
        }
    }
}
