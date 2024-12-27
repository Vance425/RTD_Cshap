using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NLog;
using RTDDAC;
using RTDWebAPI.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTDWebAPI.Controllers
{
    public class BasicController : Controller
    {
        public static ConcurrentQueue<EventQueue> eventQueue { get; set; }
        public static IConfiguration configuration { get; set; }
        public static DBPool dbPool { get; set; }
    }
}
