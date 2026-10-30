using Microsoft.AspNetCore.Mvc;
using StargateAPI.DataAccess;
using StargateAPI.Services;

namespace StargateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProcessLoggerController : ControllerBase
    {
        private readonly ProcessLogger _processLogger;

        public ProcessLoggerController(ProcessLogger processLogger)
        {
            _processLogger = processLogger;
        }

        [HttpGet("GetAllLogs")]
        public async Task<ActionResult<IReadOnlyList<ProcessLog>>> GetAllLogs(CancellationToken cancellationToken)
        {
            var logs = await _processLogger.GetAllAsync(cancellationToken);
            return Ok(logs);
        }
    }
}
