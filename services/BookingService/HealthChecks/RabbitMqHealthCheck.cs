using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BookingService.HealthChecks;

/// <summary>
/// Health check for RabbitMQ
/// </summary>
public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly ILogger<RabbitMqHealthCheck> _logger;
    private readonly string _host;
    private readonly int _port;

    /// <summary>
    /// Constructor
    /// </summary>
    public RabbitMqHealthCheck(ILogger<RabbitMqHealthCheck> logger, string host, int port)
    {
        _logger = logger;
        _host = host;
        _port = port;
    }

    /// <summary>
    /// Check if RabbitMQ is healthy
    /// </summary>
    /// <param name="context">Health check context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple ping test to check if RabbitMQ is reachable
            using var tcpClient = new System.Net.Sockets.TcpClient();
            
            var connectTask = tcpClient.ConnectAsync(_host, _port, cancellationToken);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            
            var completedTask = await Task.WhenAny(connectTask.AsTask(), timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                _logger.LogWarning("RabbitMQ health check timed out for {Host}:{Port}", _host, _port);
                return HealthCheckResult.Degraded($"Connection to RabbitMQ at {_host}:{_port} timed out");
            }

            // Ensure the connection task is completed (not faulted)
            await connectTask;
            
            if (tcpClient.Connected)
            {
                _logger.LogInformation("RabbitMQ is healthy at {Host}:{Port}", _host, _port);
                return HealthCheckResult.Healthy($"Connected to RabbitMQ at {_host}:{_port}");
            }
            else
            {
                _logger.LogWarning("Failed to connect to RabbitMQ at {Host}:{Port}", _host, _port);
                return HealthCheckResult.Unhealthy($"Failed to connect to RabbitMQ at {_host}:{_port}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ health check failed for {Host}:{Port}", _host, _port);
            return HealthCheckResult.Unhealthy($"Error connecting to RabbitMQ at {_host}:{_port}: {ex.Message}");
        }
    }
}
