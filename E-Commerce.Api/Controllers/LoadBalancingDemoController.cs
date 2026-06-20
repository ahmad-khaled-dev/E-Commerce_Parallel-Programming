using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

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
        var serverUrls = new string[count];
        for (int i = 0; i < count; i++)
        {
            var serverIndex = Interlocked.Increment(ref _roundRobinIndex) % Servers.Length;
            if (serverIndex < 0) serverIndex += Servers.Length;
            serverUrls[i] = Servers[serverIndex];
        }

        var tasks = new Task<object>[count];
        for (int i = 0; i < count; i++)
        {
            var taskNumber = i + 1;
            var serverUrl = serverUrls[i];
            tasks[i] = Task.Run(async () =>
            {
                var port = new Uri(serverUrl).Port;
                Log.Information("Request {TaskNumber} STARTED at {Timestamp} on port {Port}", taskNumber, DateTime.UtcNow, port);
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetStringAsync(serverUrl);
                Log.Information("Request {TaskNumber} COMPLETED at {Timestamp} on port {Port}", taskNumber, DateTime.UtcNow, port);
                return (object)new { taskNumber, routedTo = serverUrl, response };
            });
        }

        var results = (await Task.WhenAll(tasks)).ToList();

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
