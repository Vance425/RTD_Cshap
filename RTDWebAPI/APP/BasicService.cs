using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NLog;
using RTDWebAPI.Commons.Method.Database;
using RTDWebAPI.Interface;
using RTDWebAPI.Models;
using RTDWebAPI.Service;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTDWebAPI.APP
{
    public class BasicService
    {
        public ConcurrentQueue<EventQueue> _eventQueue { get; set; }
        public Dictionary<string, string> _threadConntroll { get; set; }
        public Dictionary<string, string> _alarmDetail { get; set; }
        public IConfiguration _configuration { get; set; }
        public DBTool _dbTool { get; set; }
        public DBTool _dbAPI { get; set; }
        public List<DBTool> _listDBSession { get; set; }
        public ConcurrentQueue<int> _DBSessionQueue { get; set; }

        public static IBaseDataService _BaseDataService = new BaseDataService();
        public Dictionary<string, object> _uiDataCatch { get; set; }
        public int _inUse { get; set; }

        public ILogger _logger { get; set; }

        public static bool _isInitial = true;
        public static bool IsInitial
        {
            get { return _isInitial; }
            set { _isInitial = value; }
        }

        public static bool _debugMode = false;
        public static bool DebugMode
        {
            get { return _debugMode; }
            set { _debugMode = value; }
        }

        public static int _excuteMode = 0;
        public static int ExcuteMode
        {
            get { return _excuteMode; }
            set { _excuteMode = value; }
        }

        public bool _IsAlive = false;
        public bool IsAlive
        {
            get { return _IsAlive; }
            set { _IsAlive = value; }
        }
        public IFunctionService _functionService { get; set; }

        public static Global global { get; set; }

        public int ipass = 0;
    }
}
