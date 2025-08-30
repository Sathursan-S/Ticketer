using BookingService.Application.Sagas;
using BookingService.HealthChecks;
using BookingService.Infrastructure.Messaging;
using BookingService.Infrastructure.Persistence.Saga;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// saga state db context
builder.Services.AddDbContext<BookingSagaDbContext>(options =>
{
    if (builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
    {
        options.UseInMemoryDatabase("BookingServiceDb");
    }
    else
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresSQL"), npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });
    }
});

// Configure RabbitMQ settings
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

// MassTransit setup
builder.Services.AddMassTransit(x =>
{
    // Configure RabbitMQ
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqSettings = context.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
        
        cfg.Host(rabbitMqSettings.Host, rabbitMqSettings.VirtualHost, h =>
        {
            h.Username(rabbitMqSettings.Username);
            h.Password(rabbitMqSettings.Password);
        });

        // Configure endpoints by convention
        cfg.ConfigureEndpoints(context);
    });

    // Configure saga state machine
    x.AddSagaStateMachine<BookingStateMachine, BookingState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.ExistingDbContext<BookingSagaDbContext>();
            r.UsePostgres();
        });

    // Auto-discover consumers and message handlers in the assembly
    x.SetKebabCaseEndpointNameFormatter();
    x.AddSagas(typeof(Program).Assembly);
    x.AddConsumers(typeof(Program).Assembly);
});

// Register RabbitMQ health check service
builder.Services.AddSingleton(serviceProvider => 
{
    var rabbitMqSettings = serviceProvider.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    var logger = serviceProvider.GetRequiredService<ILogger<RabbitMqHealthCheck>>();
    return new RabbitMqHealthCheck(logger, rabbitMqSettings.Host, rabbitMqSettings.Port);
});

// Register health checks
builder.Services.AddHealthChecks()
    .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: new[] { "services", "messaging" });

var app = builder.Build();

// Add health checks endpoint
app.MapHealthChecks("/health/rabbitmq", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("messaging"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.ToString()
            })
        };
        
        await System.Text.Json.JsonSerializer.SerializeAsync(
            context.Response.Body, 
            result, 
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
});

await app.RunAsync();