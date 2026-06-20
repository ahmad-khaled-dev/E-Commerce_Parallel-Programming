var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
var app = builder.Build();

string[] Backends = { "http://localhost:5162", "http://localhost:5163" };
var roundRobinIndex = -1;
var requestCounter = 0;

app.MapGet("/route", async () =>
{
    var requestNumber = Interlocked.Increment(ref requestCounter);
    Console.WriteLine($"[Load Balancer] Request #{requestNumber} STARTED at {DateTime.Now:HH:mm:ss.fff}");
    var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    var startIndex = Interlocked.Increment(ref roundRobinIndex);
    var anySkipped = false;

    for (var i = 0; i < Backends.Length; i++)
    {
        var index = (startIndex + i) % Backends.Length;
        if (index < 0) index += Backends.Length;
        var backend = Backends[index];

        Console.WriteLine($"[Load Balancer] Checking backend: {backend}");

        try
        {
            var healthResponse = await client.GetAsync($"{backend}/api/load-balancing/health");
            if (!healthResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Load Balancer] Backend {backend} UNHEALTHY (status: {(int)healthResponse.StatusCode})");
                anySkipped = true;
                continue;
            }

            Console.WriteLine($"[Load Balancer] Backend {backend} HEALTHY");
            var instanceInfo = await client.GetStringAsync($"{backend}/api/load-balancing/instance-info");
            Console.WriteLine($"[Load Balancer] Request #{requestNumber} COMPLETED at {DateTime.Now:HH:mm:ss.fff} -> routed to {backend}");
            Console.WriteLine($"[Load Balancer] Routing to: {backend}");

            return Results.Ok(new
            {
                routedTo = backend,
                healthCheckSkipped = anySkipped,
                response = instanceInfo
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Load Balancer] Backend {backend} UNHEALTHY (exception: {ex.Message})");
            anySkipped = true;
        }
    }

    Console.WriteLine("[Load Balancer] No healthy backends available");
    return Results.StatusCode(503);
});

app.Run();
