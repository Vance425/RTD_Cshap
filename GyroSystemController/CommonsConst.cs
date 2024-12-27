/**
 * Common.Const.cs
  **Realtime Assistive Equipment Production Vehicle Dispatching System
  *** define const
  *** 
  **/

namespace GyroLibrary
{
    public class GyroComponent
    {
        public const string GyroRTD = @"
        RRRRRRR     TTTTTTTTTT   DDDDDDDD
       RR     RR       TT     　DD      DD
      RR     RR       TT     　DD       DD
     RR   RRR       TT     　DD       DD
    RR RRR         TT     　DD       DD
   RR    RReal    TTime  　DD      DD
  RR      RR     TT     　DD    DD
RRRRRRRRRRRRRRRTTT       DDDDDDDispatcher  System {0} @ Gyro System Inc.";
    }

    public class CommonsConst
    {
        public const string AlarmDetail = "AlarmDetail";
        public const string BaseComponent = "BaseComponent";
        public const string BaseDataServices = "BaseDataServices";
        public const string Configuration = "Configuration";
        public const string DATA = "DATA";
        public const string EventQueue = "EventQueue";
        public const string SingleEventQueue = "SingleEventQueue";
        public const string FunctionService = "FunctionService";
        public const string ILogger = "ILogger";
        public const string lstDBSession = "lstDBSession";
        public const string ThreadsControll = "ThreadsControll";
        public const string ThreadsOutControll = "ThreadsOutControll";
        public const string UIDataCtrl = "UIDataCtrl";
        public const string DebugMode = "DebugMode";
        public const string ListEquipment = "ListEquipment";
        public const string Machine = "Machine";
        public const string CustomerName = "CustomerName";
        public const string ResultCode = "ResultCode";
        public const string Mqtt = "Mqtt";
        public const string RTDSecKey = "RTDSecKey";
        public const string DBRequsetTurnon = "DBRequsetTurnon";
    }

    public class CommonsData
    {
        public const string DBTool = "DBTool";
        public const string DataTable = "DataTable";
        public const string LotID = "LotID";
        public const string CarrierID = "CarrierID";
        public const string VehicleID = "VehicleID";
        public const string EquipmentID = "EquipmentID";
        public const string PortID = "PortID";
        public const string ListNormalTransfer = "lstNormalTransfer";
        public const string Model = "Model";
        public const string Quantity = "Quantity";
        public const string Total = "Total";
        public const string UserID = "UserID";
        public const string Pwd = "Pwd";
        public const string DeviceID = "DeviceID";
        public const string TotalFoup = "TotalFoup";
        public const string EquipmentMode = "EquipmentMode";
        public const string RemoteCmdKey = "RemoteCmdKey";
        public const string CommandID = "CommandID";
        public const string Key = "Key";
        public const string Time = "Time";
        public const string LastDatetime = "LastDatetime";
    }

    public class CommonsSchedule
    {
        public const string Timer = "iTimer";
        public const string TimeUnit = "timeUnit";
        public const string ScheduleName = "scheduleName";
        public const string TimerID = "timerID";
    }

    public class CommonsTimeUnit
    {
        public const string day = "day";
        public const string hours = "hours";
        public const string minutes = "minutes";
        public const string seconds = "seconds";
        public const string milliseconds = "milliseconds";
        public const string symbol = "symbol";
    }

    public class CommonsReturn
    {
        public const string tmpMsg = "tmpMsg";
    }

    public class CommonsRTDAlarm
    {
        public const string CommandID = "CommandID";
        public const string Params = "Params";
        public const string Desc = "Description";
        public const string EventTrigger = "EventTrigger";
        public const string AlarmCode = "AlarmCode";
    }

    public class CommonsInfoUpdate
    {
        public const string LotID = "LotID";
        public const string Stage = "Stage";
        public const string CarrierID = "CarrierID";
        public const string Cust = "Cust";
        public const string PartID = "PartID";
        public const string LotType = "LotType";
        public const string Automotive = "Automotive";
        public const string State = "State";
        public const string HoldCode = "HoldCode";
        public const string TurnRatio = "TurnRatio";
        public const string EOTD = "EOTD";
        public const string POTD = "POTD";
        public const string HoldReas = "HoldReas";
        public const string WaferLot = "WaferLot";
        public const string Force = "Force";
        public const string TempColumn1 = "TempColumn1";
        public const string TempColumn2 = "TempColumn2";
    }

    public class TravelCard
    {
        public const string LotID = "LotID";
        public const string CurrentLotStage = "CurrentLotStage";
        public const string LastLotStage = "LastLotStage";
        public const string LotState = "LotState";
        public const string CustomerName = "CustomerName";
        public const string CarrierID = "CarrierID";
        public const string CarrierState = "CarrierState";
        public const string CarrierInfo = "CarrierInfo";
        public const string EquipmentID = "EquipmentID";
        public const string PortModel = "PortModel";
        public const string PortState = "PortState";
        public const string Workgroup = "Workgroup";
        public const string Recipe = "Recipe";
        public const string CustomerDevice = "CustomerDevice";
    }

    public class Configuration
    {
        public const string AppSettings = "AppSettings";
        public const string _AppSettings_Secret = "AppSettings:Secret";
        public const string _AppSettings_Work = "AppSettings:Work";
        public const string _AppSettings_Server = "AppSettings:Server";

        public const string Logging = "Logging";
        public const string _Logging_LogLevel = "Logging:LogLevel";
        public const string _Logging_LogLevel_Default = "Logging:LogLevel:Default";
        public const string _Logging_LogLevel_Microsoft = "Logging:LogLevel:Microsoft";
        public const string _Logging_LogLevel_Microsoft_Hosting_Lifetime = "Logging:LogLevel:Microsoft.Hosting.Lifetime";

        public const string ThreadPools = "ThreadPools";
        public const string ThreadPools_minThread = "ThreadPools:minThread";
        public const string ThreadPools_minThread_workerThread = "ThreadPools:minThread:workerThread";
        public const string ThreadPools_minThread_portThread = "ThreadPools:minThread:portThread";
        public const string ThreadPools_maxThread = "ThreadPools:maxThread";
        public const string ThreadPools_maxThread_workerThread = "ThreadPools:maxThread:workerThread";
        public const string ThreadPools_maxThread_portThread = "ThreadPools:maxThread:portThread";
        public const string ThreadPools_UIThread = "ThreadPools:UIThread";

        public const string ReflushTime = "ReflushTime";
        public const string ReflushTime_CheckQueryAvailableTester = "ReflushTime:CheckQueryAvailableTester";
        public const string ReflushTime_CheckQueryAvailableTester_Time = "ReflushTime:CheckQueryAvailableTester:Time";
        public const string ReflushTime_CheckQueryAvailableTester_TimeUnit = "ReflushTime:CheckQueryAvailableTester:TimeUnit";
        public const string ReflushTime_ReflusheRack = "ReflushTime:ReflusheRack";
        public const string ReflushTime_ReflusheRack_Time = "ReflushTime:ReflusheRack:Time";
        public const string ReflushTime_ReflusheRack_TimeUnit = "ReflushTime:ReflusheRack:TimeUnit";

        public const string AllowedHosts = "AllowedHosts";

        public const string DBConnect = "DBConnect";
        public const string DBConnect_Type = "DBConnect:{0}";
        public const string DBConnect_Type_Name = "DBConnect:{0}:Name";
        public const string DBConnect_Type_IP = "DBConnect:{0}:IP";
        public const string DBConnect_Type_Port = "DBConnect:{0}:Port";
        public const string DBConnect_Type_User = "DBConnect:{0}:User";
        public const string DBConnect_Type_Pwd = "DBConnect:{0}:Pwd";
        public const string DBConnect_Type_ConnectionString = "DBConnect:{0}:ConnectionString";
        public const string DBConnect_Type_ProviderName = "DBConnect:{0}:providerName";
        public const string DBConnect_Type_AutoDisconnect = "DBConnect:{0}:autoDisconnect";

        public const string MCSSetting = "MCS";
        public const string MCSSetting_IP = "MCS:ip";
        public const string MCSSetting_Port = "MCS:port";
        public const string MCSSetting_TimeSpan = "MCS:timeSpan";
        public const string MCSSetting_Zone = "MCS:zone";

        public const string RTDHome = "RTDHome";
        public const string RTDHome_Url = "RTDHome:url";
    }

    public class SendToMCS
    {
        public const string Model = "Model";
        public const string EquipmentStatusSync = "EquipmentStatusSync";
        public const string ReworkMode = "ReworkMode";
    }

    public class ConstParams
    {
        public const string Parameter = "Parameter";
        public const string Paramtype = "Paramtype";
        public const string Paramvalue = "Paramvalue";
        public const string Modifyby = "Modifyby";
        public const string LastModify_dt = "LastModify_dt";
        public const string Description = "Description";
    }
}
