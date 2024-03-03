using Microsoft.AspNetCore.Mvc;
using Orchestrator.Models;
using Orchestrator.Services.Execution;

namespace Orchestrator.Controllers.ManualExecution.v1
{
    [Route("api/v1/[controller]/[action]")]
    [ApiController]
    public class ManualExecution : ControllerBase
    {
        private readonly Execute _execute;

        public ManualExecution(Execute execute)
        {
            _execute = execute;
        }


        [HttpPost]
        public async Task<IActionResult> ExecuteProgram([FromBody] string? arg)
        {
            var result = await _execute.DoInference(new ApplicationInput()
            {
                Input = arg ?? "hello world"
            });
            return Ok(result);
        }
    }
}
