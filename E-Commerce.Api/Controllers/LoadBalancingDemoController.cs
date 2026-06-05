using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Api.Controllers;

[ApiController]
[Route("api/load-balancing")]
public class LoadBalancingDemoController : ControllerBase
{
    [HttpGet("single-instance")]
    public async Task<IActionResult> SingleInstance()
    {
        await Task.Delay(2000);

        return Ok(new
        {
            port = HttpContext.Connection.LocalPort,
            machineName = Environment.MachineName,
            threadId = Environment.CurrentManagedThreadId,
            timestamp = DateTime.UtcNow,
            message = "All requests handled by ONE instance — bottleneck!"
        });
    }

    [HttpGet("instance-info")]
    public IActionResult InstanceInfo()
    {
        var instanceId = Guid.NewGuid();

        return Ok(new
        {
            port = HttpContext.Connection.LocalPort,
            machineName = Environment.MachineName,
            threadId = Environment.CurrentManagedThreadId,
            timestamp = DateTime.UtcNow,
            instanceId,
            message = $"This request was handled by instance {instanceId} — run two instances on different ports to see load distribution!"
        });
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            machineName = Environment.MachineName
        });
    }
}
