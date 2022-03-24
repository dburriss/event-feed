using Microsoft.AspNetCore.Mvc;

namespace EventFeed.Example1.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TemperatureController : ControllerBase
    {
        private readonly TemperatureData _data;
        private readonly ILogger<TemperatureController> _logger;

        public TemperatureController(TemperatureData data, ILogger<TemperatureController> logger)
        {
            _data = data;
            _logger = logger;
        }

        [HttpGet(Name = "GetTemps")]
        public IEnumerable<Temp> Get()
        {
            return _data.Get();
        }

        [HttpPost(Name = "RecordTemperature")]
        public void Post(float temp, DateTime dateTime)
        {
            _data.SaveTemp(temp, dateTime);
        }
    }
}