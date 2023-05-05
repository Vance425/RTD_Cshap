using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTDWebAPI.Commons.Method.TestService
{
    public class classTestService
    {
        public interface IMService
        {
            string GetString();
        }

        public class MService : IMService
        {
            public string GetString()
            {
                return "这是我的服务";
            }
        }
    }
}
