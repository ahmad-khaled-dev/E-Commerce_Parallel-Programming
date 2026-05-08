using E_Commerce.Application.interfaces;
using E_Commerce.Application.Interfaces;
using E_Commerce.Infrastructure.BackgroundJobs;
using E_Commerce.Infrastructure.Persistence;
using E_Commerce.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ── Requirement 3: Asynchronous Queues ───────────────────────────────────────
// Singleton so the same Channel is shared between producers (controllers)
// and the consumer (NotificationWorker background service).
builder.Services.AddSingleton<INotificationQueue, NotificationQueue>();
builder.Services.AddHostedService<NotificationWorker>();

// ── Requirement 4: Batch Processing ─────────────────────────────────────────
// Scoped processor (needs AppDbContext). DailySalesBatchJob creates its
// own scope per run via IServiceScopeFactory to avoid captive-dependency issues.
builder.Services.AddScoped<IBatchSalesProcessor, BatchSalesProcessor>();
builder.Services.AddHostedService<DailySalesBatchJob>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(context);
}


app.Run();