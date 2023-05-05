using HttpClientCallJWTAPI.DTOs;
using HttpClientCallJWTAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RTDWebAPI.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RTDWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JWTLoginController : BasicController
    {

        private readonly ILogger<JWTLoginController> _logger;
        HttpClientCallJWTAPI.DTOs.APIResult foo;

        public JWTLoginController(ILogger<JWTLoginController> logger)
        {
            _logger = logger;
        }

        public Task<HttpClientCallJWTAPI.DTOs.APIResult> PostAsync([FromBody] userLoginData value)
        {
            return PostAsync(value, foo);
        }

        [HttpPost]
        public async Task<HttpClientCallJWTAPI.DTOs.APIResult> PostAsync([FromBody] userLoginData value, HttpClientCallJWTAPI.DTOs.APIResult foo)
        {
            //HttpClientCallJWTAPI.DTOs.APIResult foo;
            string tmpMsg = "";

            LoginManager loginManager = new LoginManager();
            Output("進行登入身分驗證");
            HttpClientCallJWTAPI.DTOs.APIResult result = await loginManager.PostAsync(new LoginRequestDTO()
            {
                Account = "user50",
                Password = "password50"
            });
            if (result.Status == true)
            {
                Console.WriteLine($"登入成功");
                Console.WriteLine($"{result.Payload}");
            }
            else
            {
                Console.WriteLine($"登入失敗");
                Console.WriteLine($"{result.Message}");
            }
            Thread.Sleep(2000);

            Output("利用取得的 JTW Token 呼叫取得部門資訊 Web API");
            DepartmentsManager departmentsManager = new DepartmentsManager();
            result = await departmentsManager.GetAsync(loginManager.SingleItem.Token);
            if (result.Status == true)
            {
                Console.WriteLine($"取得部門資料成功");
                Console.WriteLine($"{result.Payload}");
            }
            else
            {
                Console.WriteLine($"取得部門資料失敗");
                Console.WriteLine($"{result.Message}");
            }

            Console.WriteLine("等候10秒鐘，等待 JWT Token 失效");
            await Task.Delay(10000);

            departmentsManager = new DepartmentsManager();
            Output("再次呼叫取得部門資訊 Web API，不過，該 JWT Token已經失效了");
            result = await departmentsManager.GetAsync(loginManager.SingleItem.Token);
            if (result.Status == true)
            {
                Console.WriteLine($"取得部門資料成功");
                Console.WriteLine($"{result.Payload}");
            }
            else
            {
                Console.WriteLine($"取得部門資料失敗");
                Console.WriteLine($"{result.Message}");
            }
            Thread.Sleep(2000);

            RefreshTokenService refreshTokenService = new RefreshTokenService();
            Output("呼叫更新 JWT Token API，取得更新的 JWT Token");
            result = await refreshTokenService.GetAsync(loginManager.SingleItem.RefreshToken);
            if (result.Status == true)
            {
                Console.WriteLine($"更新 JWT Token 成功");
                Console.WriteLine($"{result.Payload}");
            }
            else
            {
                Console.WriteLine($"更新 JWT Token 失敗");
                Console.WriteLine($"{result.Message}");
            }
            Thread.Sleep(2000);

            departmentsManager = new DepartmentsManager();
            Output("再次呼叫取得部門資訊 Web API，不過，使用剛剛取得的更新 JWT Token");
            result = await departmentsManager.GetAsync(refreshTokenService.SingleItem.Token);
            if (result.Status == true)
            {
                Console.WriteLine($"取得部門資料成功");
                Console.WriteLine($"{result.Payload}");
            }
            else
            {
                Console.WriteLine($"取得部門資料失敗");
                Console.WriteLine($"{result.Message}");
            }
            Thread.Sleep(2000);

            Console.WriteLine("Press any key for continuing...");
            Console.ReadKey();

            _logger.LogInformation(string.Format("Info:{0}",tmpMsg));
            _logger.LogWarning(string.Format("Warning:{0}", tmpMsg));
            _logger.LogError(string.Format("Error:{0}", tmpMsg));
            _logger.LogDebug(string.Format("Debug:{0}", tmpMsg));
            _logger.LogCritical(string.Format("Critical:{0}", tmpMsg));

            //string sql = "select * from gyro_lot_carrier_associate";
            //DataSet ds = dbPool.GetDataSet(sql);

            return foo;
        }

        public static void Output(string message)
        {
            Console.WriteLine(message);
            Thread.Sleep(2000);
        }
    }

    public class JWTLoginData
    {
        public string UserID { get; set; }
        public string Department { get; set; }
        public string Pwd { get; set; }
    }
}
