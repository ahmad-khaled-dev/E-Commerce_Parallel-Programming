using System.Net.Http;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Api.Controllers;

[ApiController]
[Route("api/load-balancing")]
public class LoadBalancingDemoController : ControllerBase
{
    private static readonly string[] Servers = {
        "http://localhost:5162/api/load-balancing/instance-info",
        "http://localhost:5163/api/load-balancing/instance-info"
    };

    private static int _roundRobinIndex;

    private readonly IHttpClientFactory _httpClientFactory;

    public LoadBalancingDemoController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("distribute-requests")]
    public async Task<IActionResult> DistributeRequests([FromQuery] int count)
    {
        var results = new List<object>();

        for (int i = 1; i <= count; i++)
        {
            var serverIndex = Interlocked.Increment(ref _roundRobinIndex) % Servers.Length;
            if (serverIndex < 0) serverIndex += Servers.Length;
            var serverUrl = Servers[serverIndex];

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetStringAsync(serverUrl);

            results.Add(new
            {
                taskNumber = i,
                routedTo = serverUrl,
                response
            });
        }

        return Ok(new
        {
            totalTasks = count,
            distribution = results
        });
    }

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
