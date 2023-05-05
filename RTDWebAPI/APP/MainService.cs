using Microsoft.Extensions.Configuration;
using NLog;
using RTDWebAPI.Commons.Method.Database;
using RTDWebAPI.Commons.Method.WSClient;
using RTDWebAPI.Interface;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace RTDWebAPI.APP
{
    public class MainService:BasicService
    {
        public void Start()
        {
            int iTime = 0;
            _IsAlive = true;
            string tmpMsg = String.Empty;
            string curProcessName = "MainService";

            tmpMsg = String.Format("{0} is Start", curProcessName);
            Console.WriteLine(tmpMsg);

            EventLogicService evtLogicService = new EventLogicService();

            try
            {
                while (true)
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

                        Task taskEvtLogicService = Task.Run(() =>
                        {
                            evtLogicService.Start();
                            Console.WriteLine($"hello, taskEvtLogicService{ Thread.CurrentThread.ManagedThreadId}");
                        });
                    }

                    Thread.Sleep(1000);    
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
