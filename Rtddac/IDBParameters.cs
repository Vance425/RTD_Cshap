/**
 * IDBParameters.cs
  **RTD Data Access (RTDdac)
  *** IDBParameters
  *** 
  **/
namespace RTDDAC
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
