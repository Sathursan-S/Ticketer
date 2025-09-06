using PaymentService.Application.Services;
using PaymentService.Application.Gateways;
using MassTransit;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using PaymentService.Application.Consumers;
using SharedLibrary.Infrastructure;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;
using OpenTelemetry.Exporter.Jaeger;

var builder = WebApplication.CreateBuilder(args);

// Configure RabbitMQ settings
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMq"));

// Configure Stripe settings
builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Payment Service API",
        Version = "v1",
        Description = "API for managing payments, refunds, and webhooks with multiple payment gateways",
        Contact = new OpenApiContact
        {
            Name = "Support",
            Email = "support@paymentservice.com"
        }
    });
});

// MassTransit configuration
builder.Services.AddMassTransit(config =>
{
    config.AddConsumer<ProcessPaymentConsumer>();
    config.SetKebabCaseEndpointNameFormatter();

    config.UsingRabbitMq((context, cfg) =>
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

// Register payment gateways
builder.Services.AddScoped<IPaymentGateway, StripePaymentGateway>();

// Register payment service
builder.Services.AddScoped<IPaymentService, PaymentService.Application.Services.PaymentService>();

// Add health checks
builder.Services.AddHealthChecks();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("PaymentService"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("MassTransit")
        .AddJaegerExporter(options =>
        {
            var jaegerEndpoint = builder.Configuration["OpenTelemetry:Jaeger:Endpoint"];
            if (!string.IsNullOrEmpty(jaegerEndpoint))
            {
                options.AgentHost = jaegerEndpoint;
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

var app = builder.Build();

// Configure middleware
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service API V1"); });

// Map health check endpoints
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health");

// Add Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint("/metrics");

await app.RunAsync();