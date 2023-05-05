using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTDWebAPI.Commons.Method.Database
{
    public interface IDBParameters
    {
        string ParamChar { get; }
        string SysDateTimeString { get; }
        string SysDateTime { get; }
        string PlusChar { get; }
        string Fromdual { get; }
        string DBNullReplace { get; }
        string SubChar { get; }
    }
}
