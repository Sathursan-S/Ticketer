using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using HealthChecks.Redis;
using System.Text.Json;

namespace TicketService.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();
        
        // Check the API itself is running
        healthChecksBuilder.AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "api" });
        
        // Add database health check
        healthChecksBuilder.AddDbContextCheck<TicketDbContext>("database", 
            tags: new[] { "db", "data", "ready" });
            
        // Add Redis health check
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        healthChecksBuilder.AddRedis(redisConnectionString, "redis", 
            tags: new[] { "redis", "cache", "ready" });
            
        // RabbitMQ health check is registered separately in Program.cs

        return services;
    }

    public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                
                var response = new
                {
                    Status = report.Status.ToString(),
                    Duration = report.TotalDuration,
                    Checks = report.Entries.Select(e => new
                    {
                        Name = e.Key,
                        Status = e.Value.Status.ToString(),
                        Duration = e.Value.Duration,
                        Description = e.Value.Description
                    })
                };
                
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        });

        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                
                var response = new
                {
                    Status = report.Status.ToString(),
                    Duration = report.TotalDuration,
                    Checks = report.Entries.Select(e => new
                    {
                        Name = e.Key,
                        Status = e.Value.Status.ToString(),
                        Duration = e.Value.Duration
                    })
                };
                
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        });

        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = async (context, report) =>
            {
                await context.Response.WriteAsync("Healthy");
            }
        });

        return app;
    }
}
