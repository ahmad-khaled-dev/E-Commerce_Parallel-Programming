using E_Commerce.Application.interfaces;
using E_Commerce.Application.Interfaces;
using E_Commerce.Infrastructure.BackgroundJobs;
using E_Commerce.Infrastructure.Decorators;
using E_Commerce.Infrastructure.Persistence;
using E_Commerce.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Information("App started");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AppDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<IProductService>(sp =>
{
    var inner = sp.GetRequiredService<ProductService>();
    var logger = sp.GetRequiredService<ILogger<LoggingProductServiceDecorator>>();
    return new LoggingProductServiceDecorator(inner, logger);
});

builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<IInventoryService>(sp =>
{
    var inner = sp.GetRequiredService<InventoryService>();
    var logger = sp.GetRequiredService<ILogger<LoggingInventoryServiceDecorator>>();
    return new LoggingInventoryServiceDecorator(inner, logger);
});

builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddSingleton<INotificationQueue, NotificationQueue>();
builder.Services.AddHostedService<NotificationWorker>();
builder.Services.AddTransient<BatchSalesProcessor>();
builder.Services.AddTransient<IBatchSalesProcessor>(sp =>
{
    var inner = sp.GetRequiredService<BatchSalesProcessor>();
    var logger = sp.GetRequiredService<ILogger<LoggingBatchSalesProcessorDecorator>>();
    return new LoggingBatchSalesProcessorDecorator(inner, logger);
});
builder.Services.AddHostedService<DailySalesBatchJob>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(context);
}

app.Run();