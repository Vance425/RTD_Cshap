using Commons.Method.Tools;
using GyroLibrary;
using Microsoft.Extensions.Configuration;
using NLog;
using RTDDAC;
using RTDWebAPI.Commons.Method.WSClient;
using RTDWebAPI.Interface;
using RTDWebAPI.Service;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace RTDWebAPI.APP
{
    public class MainService : BasicService
    {
        public void Start()
        {
            DBTool dbTool;
            int iTime = 0;
            _IsAlive = true;
            string tmpMsg = String.Empty;
            string curProcessName = "MainService";

            tmpMsg = String.Format("{0} is Start", curProcessName);
            Console.WriteLine(tmpMsg);

            EventLogicService evtLogicService = new EventLogicService();
            IFunctionService _functionService = new FunctionService();
            bool _changeRootsession = false;

            try
            {
                int _sleeptime = 1000;
                bool _issuestart = false;
                DateTime _laststarttime = DateTime.Now;
                DateTime _issuestarttime = DateTime.Now;
                DateTime _currenttime = DateTime.Now;
                while (true)
                {
                    _currenttime = DateTime.Now;

                    if (_listDBSession.Count > 0)
                    {
                        _dbTool = _listDBSession[0];
                    }
                    else
                    {
                        try
                        {
                            tmpMsg = "";
                            string tmpDataSource = string.Format("{0}:{1}/{2}", _configuration["DBconnect:Oracle:ip"], _configuration["DBconnect:Oracle:port"], _configuration["DBconnect:Oracle:Name"]);
                            string tmpConnectString = string.Format(_configuration["DBconnect:Oracle:connectionString"], tmpDataSource, _configuration["DBconnect:Oracle:user"], _configuration["DBconnect:Oracle:pwd"]);
                            string tmpDatabase = _configuration["DBConnect:Oracle:providerName"];
                            string tmpAutoDisconn = _configuration["DBConnect:Oracle:autoDisconnect"];

                            _dbTool = new DBTool(tmpConnectString, tmpDatabase, tmpAutoDisconn, out tmpMsg);
                            _dbTool._dblogger = _logger;

                            if (!tmpMsg.Equals(""))
                            {
                                if (_issuestart.Equals(false))
                                {
                                    _issuestarttime = DateTime.Now;
                                    _issuestart = true;
                                }

                                if (_functionService.TimerTool("minutes", _issuestarttime.ToString("yyyy/MM/dd HH:mm:ss")) > 60)
                                {
                                    _sleeptime = 1000 * 60 * 10;
                                }
                                else if (_functionService.TimerTool("minutes", _issuestarttime.ToString("yyyy/MM/dd HH:mm:ss")) > 10)
                                {
                                    _sleeptime = 1000 * 60 * 5;
                                }
                                else
                                {
                                    _sleeptime = 1000 * 60 * 1;
                                }

                                _logger.Info(string.Format("Unable to establish database session. cause:[{0}]", tmpMsg));
                            }
                            else
                            {
                                _issuestart = false;
                                _sleeptime = 1000;
                                _listDBSession.Add(_dbTool);
                            }
                        }
                        catch (Exception ex) { }
                    }

                    tmpMsg = "";

                    try
                    {

                        if (_dbTool.IsConnected)
                        {

                            if (!evtLogicService.IsAlive)
                            {
                                evtLogicService = new EventLogicService();
                                
                                evtLogicService._logger = _logger;
                                evtLogicService._eventQueue = _eventQueue;
                                evtLogicService._threadConntroll = _threadConntroll;
                                evtLogicService._dbTool = _dbTool;
                                evtLogicService._configuration = _configuration;
                                evtLogicService._listDBSession = _listDBSession;
                                evtLogicService._uiDataCatch = _uiDataCatch;
                                evtLogicService._alarmDetail = _alarmDetail;
                                evtLogicService._payload = _payload;

                                Task taskEvtLogicService = Task.Run(() =>
                                {
                                    evtLogicService.Start();
                                    tmpMsg = $"hello, taskEvtLogicService{ Thread.CurrentThread.ManagedThreadId}";
                                    Console.WriteLine(tmpMsg);
                                    tmpMsg = string.Format("Critical issue. RTD Event Logic Service down at [{0}].", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                                    _logger.Info(tmpMsg);

                                    string _Seckey = RTDSecurity.GenerateSecurityKey();
                                    string _OriSecKey = (string)((Dictionary<string, object>)_payload)[CommonsConst.RTDSecKey];
                                    RTDSecurity.RegisterSecurityKey(_dbTool, _Seckey);
                                    ((Dictionary<string, object>)_payload)[CommonsConst.RTDSecKey] = _Seckey;
                                    _logger.Info(string.Format("{0} Security Key Change from {1} to {2}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), _OriSecKey, _Seckey));
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                    Thread.Sleep(_sleeptime);

                    if (_changeRootsession)
                    {

                        _changeRootsession = false;
                    }
                }

            }
            catch
            {
                _IsAlive = false;
            }
            finally
            {
                string stopTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine(string.Format("System Stop at [{0}]", stopTime));
            }
        }

        static void eventGo(object test)
        {
            //To Be

        }
    }
}
