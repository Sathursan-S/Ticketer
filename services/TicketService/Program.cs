using System.Reflection;
using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Exporter;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using TicketService;
using TicketService.Application.Consumers;
using TicketService.Application.Services;
using TicketService.Extensions;
using TicketService.HealthChecks;
using TicketService.Infrastructure.Messaging;
using TicketService.Mappers;
using TicketService.Repositoy;
using TicketService.Repository;
using TicketService.Consumers;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using System.Diagnostics;
using System.Diagnostics.Metrics;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EventCreatedConsumer>();
    x.AddConsumer<HoldTicketsConsumer>();

    x.SetKebabCaseEndpointNameFormatter();

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "rabbitmq";
        var user = builder.Configuration["RabbitMq:Username"] ?? "guest";
        var pass = builder.Configuration["RabbitMq:Password"] ?? "guest";
        var vhost = builder.Configuration["RabbitMq:VirtualHost"] ?? "/";

        cfg.Host(host, vhost, h =>
        {
            h.Username(user);
            h.Password(pass);
        });
        cfg.ClearSerialization();
        cfg.UseRawJsonSerializer();
        cfg.UseRawJsonDeserializer();

        cfg.ConfigureEndpoints(context);
    });
});

// Add services to the container.
builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    // Configure JSON serialization
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Add API documentation with Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ticket Service API",
        Version = "v1",
        Description = "API for managing event tickets",
        Contact = new OpenApiContact
        {
            Name = "Support",
            Email = "support@ticketservice.com"
        }
    });

    // Include XML comments in Swagger
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000", // React dev server
                "http://localhost:8080"  // Vue dev server
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Location");
    });

    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Configure database based on environment
builder.Services.AddDbContext<TicketDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var databaseProvider = builder.Configuration["DatabaseProvider"];

    if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        // Default to PostgreSQL for production
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });
    }
});

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(MapperProfile));

// Add Redis configuration with improved resilience
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

    // Configure Redis with better resilience options
    var options = ConfigurationOptions.Parse(redisConnectionString);
    options.AbortOnConnectFail = false; // Don't crash on startup if Redis is down
    options.ConnectRetry = 5;
    options.ConnectTimeout = 5000;
    options.SyncTimeout = 3000;

    return ConnectionMultiplexer.Connect(options);
});

// Add RedLock distributed lock with support for multiple Redis instances
builder.Services.AddSingleton<RedLockFactory>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionMultiplexer = sp.GetRequiredService<IConnectionMultiplexer>();

    // Check if multiple Redis endpoints are configured
    var endpoints = configuration.GetSection("Redis:Endpoints").Get<string[]>();

    if (endpoints != null && endpoints.Length > 0)
    {
        // Create multiplexers for all configured endpoints
        var multiplexers = new List<RedLockMultiplexer> { new(connectionMultiplexer) };

        foreach (var endpoint in endpoints)
        {
            if (!string.IsNullOrEmpty(endpoint) && endpoint != configuration.GetConnectionString("Redis"))
            {
                var options = ConfigurationOptions.Parse(endpoint);
                options.AbortOnConnectFail = false;
                multiplexers.Add(new RedLockMultiplexer(ConnectionMultiplexer.Connect(options)));
            }
        }

        return RedLockFactory.Create(multiplexers);
    }

    // Fall back to single Redis instance if no endpoints configured
    return RedLockFactory.Create(new List<RedLockMultiplexer> { new(connectionMultiplexer) });
});

// Add RabbitMQ settings
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMq"));

// Register RabbitMQ health check service
builder.Services.AddSingleton<RabbitMqHealthCheck>(serviceProvider =>
{
    var rabbitMqSettings = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>()
        ?? new RabbitMqSettings();
    var logger = serviceProvider.GetRequiredService<ILogger<RabbitMqHealthCheck>>();
    return new RabbitMqHealthCheck(logger, rabbitMqSettings.Host, rabbitMqSettings.Port);
});

// Register RabbitMQ health check
builder.Services.AddHealthChecks()
    .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: new[] { "rabbitmq", "messaging", "ready" });

// Add application services
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketService, TicketService.Application.Services.TicketService>();

// Add health checks
builder.Services.AddHealthChecks(builder.Configuration);

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "TicketService")
        .AddAttributes(new Dictionary<string, object>
        {
            ["service.instance.id"] = Environment.MachineName,
            ["deployment.environment"] = builder.Environment.EnvironmentName
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.EnrichWithHttpRequest = (activity, httpRequest) =>
            {
                activity.SetTag("http.request_id", httpRequest.Headers["X-Request-ID"].FirstOrDefault());
                activity.SetTag("http.user_agent", httpRequest.Headers["User-Agent"].FirstOrDefault());
            };
            options.EnrichWithHttpResponse = (activity, httpResponse) =>
            {
                activity.SetTag("http.status_code", httpResponse.StatusCode);
            };
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.EnrichWithHttpRequestMessage = (activity, httpRequest) =>
            {
                activity.SetTag("http.request_id", httpRequest.Headers.GetValues("X-Request-ID").FirstOrDefault());
            };
            options.EnrichWithHttpResponseMessage = (activity, httpResponse) =>
            {
                activity.SetTag("http.status_code", httpResponse.StatusCode);
            };
        })
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
            options.EnrichWithIDbCommand = (activity, command) =>
            {
                activity.SetTag("db.connection_string", command.Connection?.ConnectionString);
                activity.SetTag("db.command_type", command.CommandType.ToString());
            };
        })
        .AddRedisInstrumentation()
        .AddSource("MassTransit")
        .AddSource("TicketService.*")
        .AddSource("Ticketer.Ticket")
        .AddSource("Ticketer.Saga")
        .AddConsoleExporter()
        .AddJaegerExporter(options =>
        {
            var jaegerEndpoint = builder.Configuration["OpenTelemetry:Jaeger:Endpoint"];
            if (!string.IsNullOrEmpty(jaegerEndpoint))
            {
                options.AgentHost = jaegerEndpoint.Split(':')[0];
                options.AgentPort = int.Parse(jaegerEndpoint.Split(':')[1]);
            }
            else
            {
                options.AgentHost = "jaeger";
                options.AgentPort = 6831;
            }
        })
        .AddOtlpExporter(options =>
        {
            var otlpEndpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"];
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            }
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("TicketService.*")
        .AddMeter("Ticketer.Business.Metrics")
        .AddConsoleExporter()
        .AddOtlpExporter(options =>
        {
            var otlpEndpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"];
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            }
        })
        .AddPrometheusExporter());

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable Swagger UI in development
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Ticket Service API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });

    // Apply migrations in development for easier testing
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<TicketDbContext>();

        // Ensure database is created for SQLite
        if (builder.Configuration["DatabaseProvider"]?.ToLower() == "sqlite")
        {
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            await context.Database.MigrateAsync();
        }
    }
}
else
{
    // In production, redirect HTTP to HTTPS and use custom error handling
    app.UseHttpsRedirection();
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Use CORS before routing
app.UseCors();

// Add routing middleware
app.UseRouting();

// Authentication and authorization will be implemented in a future update

// Map controllers
app.MapControllers();

// Configure health check endpoints
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health");

// Add Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint("/metrics");

await app.RunAsync();
