using Serilog;
using Serilog.Events;
using LabApi.Configuration;
using LabApi.Endpoints;
using LabApi.Middleware;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/app-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

app.UseHttpMetrics();
app.MapMetrics();

app.UseMiddleware<CorrelationIdMiddleware>();
app.ConfigureHttpPipeline();
app.MapSystemEndpoints();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LabApi.Data.AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

Log.CloseAndFlush();