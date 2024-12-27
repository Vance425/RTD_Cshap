using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NLog;
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
    public class Global
    {
        public static bool _debugMode = false;
        public bool DebugMode
        {
            get { return _debugMode; }
            set { _debugMode = value; }
        }

        public static int _excuteMode = 0;
        public int ExcuteMode
        {
            get { return _excuteMode; }
            set { _excuteMode = value; }
        }

        private CheckLotInfo _checkLotInfo = new CheckLotInfo();
        public CheckLotInfo ChkLotInfo
        {
            get { return _checkLotInfo; }
            set { _checkLotInfo = value; }
        }
        public class CheckLotInfo
        {
            public int Time { get; set; }
            public string TimeUnit { get; set; }
        }

        private CheckQueryAvailableTester _checkQueryAvailableTestercuteMode = new CheckQueryAvailableTester();
        public CheckQueryAvailableTester CheckQueryAvailableTestercuteMode
        {
            get { return _checkQueryAvailableTestercuteMode; }
            set { _checkQueryAvailableTestercuteMode = value; }
        }
        public class CheckQueryAvailableTester
        {
            public int Time { get; set; }
            public string TimeUnit { get; set; }
        }
    }
}
