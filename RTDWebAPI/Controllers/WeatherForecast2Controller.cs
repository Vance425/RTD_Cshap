using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace demoWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecast2Controller : Controller
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecast2Controller> _logger;

        public WeatherForecast2Controller(ILogger<WeatherForecast2Controller> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast2> Get()
        {
            var rng = new Random();

            TempData["TestData"] = "test";

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast2
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        public IActionResult Index()
        {
            TempData["TestData"] = "test";
            return View();
        }
    }
}
