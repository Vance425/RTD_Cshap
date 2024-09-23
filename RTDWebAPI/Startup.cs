using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Threading.Tasks;
using RTDWebAPI.Models;
using System.Collections.Concurrent;
using System;
using RTDWebAPI.Commons.Method.Database;
using RTDWebAPI.Controllers;
using NLog.Config;
using NLog.Targets;
using NLog;
using System.Threading;
using RTDWebAPI.APP;
using System.Collections.Generic;
using RTDWebAPI.Interface;
using RTDWebAPI.Service;
using RTDWebAPI.Commons.Method.Tools;
using System.Data;

namespace RTDWebAPI
{
    public class Startup
    {
        DBPool dbPool = null;
        DBTool dbTool = null;
        DBTool dbAPI = null;
        ConcurrentQueue<EventQueue> eventQ = null;
        public IFunctionService functionService { get; set; }
        Dictionary<string, string> threadControll = new Dictionary<string, string>();
        List<DBTool> lstDBSession = new List<DBTool>();
        Dictionary<string, object> uiDataCatch = new Dictionary<string, object>();
        public Dictionary<string, string> alarmDetail = new Dictionary<string, string>();
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            string rtdVer = "Version 1.0.24.1009.1.0.1";
            string msg = "";
            string tmpMsg = "";
            //DBPool dbPool = null;
            BasicController basicController = new BasicController();
            functionService = new FunctionService();
            ILogger logger = LogManager.GetCurrentClassLogger();
            CreateLogger();

            try
            {
                try
                {
                    string startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string strSystem = String.Format(@"
        RRRRRRR     TTTTTTTTTT   DDDDDDDD
       RR     RR       TT     ¡@DD      DD
      RR     RR       TT     ¡@DD       DD
     RR   RRR       TT     ¡@DD       DD
    RR RRR         TT     ¡@DD       DD
   RR    RReal    TTime  ¡@DD      DD
  RR      RR     TT     ¡@DD    DD
RRRRRRRRRRRRRRRTTT       DDDDDDDispatcher  System {0} @ Gyro System Inc.", rtdVer);
                    Console.WriteLine(strSystem);
                    logger.Info(strSystem);
                    string systemName = Configuration["AppSettings:Sytem"];
                    msg = string.Format("{0}\r\nRealtime Dispatcher System Starting...", systemName);
                    Console.WriteLine(msg);
                    logger.Info(msg);
                    msg = string.Format("Start Time [{0}]", startTime);
                    Console.WriteLine(msg);
                    logger.Info(msg);

                    while (true)
                    {
                        //Create DB Tool
                        //dbTool = new DBTool(Configuration["DBconnect:Oracle:Name"], Configuration["DBconnect:Oracle:user"], Configuration["DBconnect:Oracle:pwd"]);
                        string tmpDataSource = string.Format("{0}:{1}/{2}", Configuration["DBconnect:Oracle:ip"], Configuration["DBconnect:Oracle:port"], Configuration["DBconnect:Oracle:Name"]);
                        string tmpConnectString = string.Format(Configuration["DBconnect:Oracle:connectionString"], tmpDataSource, Configuration["DBconnect:Oracle:user"], Configuration["DBconnect:Oracle:pwd"]);
                        string tmpDatabase = Configuration["DBConnect:Oracle:providerName"];
                        string tmpAutoDisconn = Configuration["DBConnect:Oracle:autoDisconnect"];
                        dbTool = new DBTool(tmpConnectString, tmpDatabase, tmpAutoDisconn, out msg);
                        dbTool._dblogger = logger;

                        if (!dbTool.IsConnected)
                        {
                            msg = "DB Connection failed.retry in 1 minute";
                            Console.WriteLine(msg);
                            logger.Info(msg);
                            logger.Error(msg);
                            Thread.Sleep(30000);
                            continue;
                        }

                        lstDBSession.Add(dbTool);
                        dbAPI = new DBTool(tmpConnectString, tmpDatabase, tmpAutoDisconn, out msg);
                        dbAPI._dblogger = logger;

                        msg = "Started of RTD system.";
                        logger.Error(msg);


                        if (!dbAPI.IsConnected)
                        {
                            msg = "API DB Connection failed.retry in 1 minute";
                            Console.WriteLine(msg);
                            logger.Info(msg);
                            Thread.Sleep(60000);
                            continue;
                        }
                        lstDBSession.Add(dbAPI);

                        if (alarmDetail.Count <= 0)
                        {
                            string sql = "";
                            DataTable dt = null;
                            sql = "select * from alarm_detail";
                            dt = dbTool.GetDataTable(sql);
                            string keyAlarm = "";
                            foreach (DataRow row in dt.Rows)
                            {
                                keyAlarm = string.Format("{0}{1}", row["AlarmCode"].ToString(), row["SubCode"].ToString());
                                //"AlarmCode", "AlarmText"
                                if (!alarmDetail.ContainsKey(keyAlarm))
                                    alarmDetail.Add(keyAlarm, row["AlarmText"].ToString());
                            }
                        }

                        msg = string.Format("DB Connection success.");
                        Console.WriteLine(msg);
                        logger.Info(msg);

                        tmpMsg = "COMMAND_STREAMCODE";
                        if (!OracleSequence.ExistsSequance(dbTool, tmpMsg))
                        {
                            if (OracleSequence.CreateSequence(dbTool, tmpMsg, 1, 99999))
                            {
                                msg = string.Format("Create Sequence [{0}] success.", tmpMsg);
                                Console.WriteLine(msg);
                                logger.Info(msg);
                            }
                            else
                            {
                                msg = string.Format("Create Sequence [{0}] failed.", tmpMsg);
                                Console.WriteLine(msg);
                                logger.Info(msg);
                            }
                        }

                        tmpMsg = "UID_STREAMCODE";
                        if (!OracleSequence.ExistsSequance(dbTool, tmpMsg))
                        {
                            if (OracleSequence.CreateSequence(dbTool, tmpMsg, 1, 99999999))
                            {
                                msg = string.Format("Create Sequence [{0}] success.", tmpMsg);
                                Console.WriteLine(msg);
                                logger.Info(msg);
                            }
                            else
                            {
                                msg = string.Format("Create Sequence [{0}] failed.", tmpMsg);
                                Console.WriteLine(msg);
                                logger.Info(msg);
                            }
                        }

                        break;
                    }
                }
                catch (Exception ex)
                {
                    msg = "Unknow fail when create DBTool Failed¡I";
                    Console.WriteLine(msg);
                    logger.Info(msg);
                }
                msg = String.Format("Database object has been create.");
                logger.Info(string.Format("Info: {0}", msg));

                //Create Event Queue
                eventQ = new ConcurrentQueue<EventQueue>();
                msg = String.Format("event Queue has been create.");
                logger.Info(string.Format("Info: {0}", msg));

                //Create Thread Controll 
                threadControll = new Dictionary<string, string>();
                msg = String.Format("Thred Controll Queue has been create.");
                logger.Info(string.Format("Info: {0}", msg));

                try
                {
                    MainService mainService = new MainService();
                    tmpMsg = String.Empty;

                    //while (true)
                    //{
                    if (!mainService.IsAlive)
                    {
                        mainService.IsAlive = false;
                        mainService._logger = logger;
                        mainService._dbTool = dbTool;
                        mainService._dbAPI = dbAPI;
                        mainService._eventQueue = eventQ;
                        mainService._threadConntroll = threadControll;
                        mainService._configuration = configuration;
                        mainService._functionService = functionService;
                        mainService._listDBSession = lstDBSession;
                        mainService._uiDataCatch = uiDataCatch;
                        mainService._alarmDetail = alarmDetail;


                        try
                        {
                            Task taskMainService = new Task(() =>
                            {
                                mainService.Start();
                            });

                            taskMainService.Start();

                            tmpMsg = String.Format("mainService is alive.");
                            logger.Info(string.Format("Info: {0}", tmpMsg));
                            tmpMsg = String.Format("mainService has started.");
                            logger.Info(string.Format("Info: {0}", tmpMsg));
                        }
                        catch (Exception ex)
                        {
                            tmpMsg = string.Format("Main Service create fail. Exception: {0}", ex.Message);
                            Console.WriteLine(tmpMsg);
                            logger.Debug(string.Format("Info: {0}", tmpMsg));
                        }
                    }

                    Thread.Sleep(1000);

                    if (!mainService.IsAlive)
                    {
                        tmpMsg = String.Format("MainService has interrupt.");
                        logger.Info(string.Format("Info: {0}", tmpMsg));
                    }
                    //}
                }
                catch (Exception ex)
                {
                    throw;
                }

                try
                {
                    DataTable dtTemp = null;
                    msg = "lotInfo";
                    dtTemp = functionService.GetLotInfo(lstDBSession[0], "", logger);
                    uiDataCatch.Add(msg, dtTemp);
                    msg = "carrier";
                    tmpMsg = "";
                    uiDataCatch.Add(msg, "");
                    msg = "equipment";
                    tmpMsg = "";
                    uiDataCatch.Add(msg, "");
                    msg = "workinprocess";
                    tmpMsg = "";
                    uiDataCatch.Add(msg, "");
                    msg = "alarmsMsg";
                    tmpMsg = "";
                    uiDataCatch.Add(msg, "");
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                dbPool.DisConnet(out msg);
                dbPool.Dispose();
                logger.Debug(string.Format("[Exception]: {0}", ex.Message));
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ILogger _logger = LogManager.GetCurrentClassLogger();
            services.AddSingleton<DBTool>(dbTool);
            services.AddSingleton<DBTool>(dbAPI);
            services.AddSingleton<List<DBTool>>(lstDBSession);
            // List<DBTool> lstDBSession
            services.AddSingleton<Dictionary<string, object>>(uiDataCatch);
            services.AddSingleton<ILogger>(_logger);
            services.AddSingleton<ConcurrentQueue<EventQueue>>(eventQ);
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddSingleton<IFunctionService>(functionService);
            services.AddSingleton<Dictionary<string, string>>(alarmDetail);
            // Add Cors
            services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "RTD Web API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "RTDWebAPI v1"));
            }
            else
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "RTDWebAPI v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseCors("MyPolicy");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.Run(async (context) =>
            {
                //await context.Response.WriteAsync("Hello World!!!!!");
                context.Response.Redirect(Configuration["RTDHome:url"]);
            });
        }
        public static void CreateLogger()
        {
            var config = new LoggingConfiguration();
            var rtdTarget = new FileTarget
            {
                FileName = "${basedir}/logs/${shortdate}.log",
                Layout = "${date:format=yyyy-MM-dd HH\\:mm\\:ss} [${uppercase:${level}}] ${message}",
                ArchiveAboveSize = 102400000,
            };
            rtdTarget.ArchiveFileName = "${basedir}/logs/${shortdate}.{#}.log";

            config.AddRule(LogLevel.Info, LogLevel.Warn, rtdTarget);

            var IssueTarget = new FileTarget
            {
                FileName = "${basedir}/logs/Issue_${shortdate}.log",
                Layout = "${date:format=yyyy-MM-dd HH\\:mm\\:ss} [${uppercase:${level}}] ${message}",
                ArchiveAboveSize = 102400000,
            };
            IssueTarget.ArchiveFileName = "${basedir}/logs/Issue_${shortdate}.{#}.log";

            config.AddRule(LogLevel.Error, LogLevel.Error, IssueTarget);

            var DebugTarget = new FileTarget
            {
                FileName = "${basedir}/logs/Debug_${shortdate}.log",
                Layout = "${date:format=yyyy-MM-dd HH\\:mm\\:ss} [${uppercase:${level}}] ${message}",
                ArchiveAboveSize = 102400000,
            };
            //DebugTarget.ArchiveAboveSize = 2;// 2048000;
            DebugTarget.ArchiveFileName = "${basedir}/logs/Debug_${shortdate}.{#}.log";

            config.AddRule(LogLevel.Debug, LogLevel.Debug, DebugTarget);

            LogManager.Configuration = config;
        }
    }
}
