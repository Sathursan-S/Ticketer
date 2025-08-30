namespace BookingService.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ connection settings
/// </summary>
public class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public int Port { get; set; } = 5672;
}
