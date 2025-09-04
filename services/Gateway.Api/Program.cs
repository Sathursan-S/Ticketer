using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Yarp.ReverseProxy;
using System.Reflection;
using System.IO;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;

var builder = WebApplication.CreateBuilder(args);

// CORS (from config)
var allowed = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("CorsPolicy", p =>
        p.WithOrigins(allowed)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddCheck("gateway-api", () => HealthCheckResult.Healthy());

// Enhanced Swagger for the gateway
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var swaggerConfig = builder.Configuration.GetSection("Swagger");
    var title = swaggerConfig["ApiTitle"] ?? "Ticketer API Gateway";
    var version = swaggerConfig["ApiVersion"] ?? "v1";
    var description = swaggerConfig["ApiDescription"] ?? "Gateway for Ticketer microservices";
    
    c.SwaggerDoc(version, new OpenApiInfo
    {
        Title = title,
        Version = version,
        Description = description,
        Contact = new OpenApiContact
        {
            Name = "Ticketer Support",
            Email = "support@ticketer.com"
        }
    });
    
    // Add XML documentation if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// YARP from config
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "Gateway.Api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter(options =>
        {
            var otlpEndpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"];
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            }
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter(options =>
        {
            var otlpEndpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"];
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            }
        })
        .AddPrometheusExporter());

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Configure and use Swagger
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
});

app.UseSwaggerUI(c =>
{
    // Gateway API documentation
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway API v1");
    
    // Microservices documentation
    c.SwaggerEndpoint("/api/payments/swagger/v1/swagger.json", "Payment Service v1");
    c.SwaggerEndpoint("/api/auth/v3/api-docs", "Auth Service v1");
    c.SwaggerEndpoint("/api/booking/swagger/v1/swagger.json", "Booking Service v1");
    c.SwaggerEndpoint("/api/tickets/swagger/v1/swagger.json", "Ticket Service v1");
    c.SwaggerEndpoint("/api/events/swagger/v1/swagger.json", "Events Service v1");
    c.SwaggerEndpoint("/api/notifications/swagger/v1/swagger.json", "Notification Service v1");
    
    // UI configuration
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Ticketer API Documentation";
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    c.DefaultModelsExpandDepth(0); // Hide schemas section by default
    c.EnableFilter();
    c.EnableDeepLinking();
});



// Forwarded headers if behind proxies (optional but useful)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// CORS
app.UseCors("CorsPolicy");

// Root health endpoint
app.MapGet("/", () => Results.Ok(new { status = "ok", service = "Gateway.Api" }))
   .WithName("GetRoot")
   .WithDisplayName("Gateway Root")
   .WithTags("Health")
   .WithDescription("Returns status information about the gateway");

// Health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var result = System.Text.Json.JsonSerializer.Serialize(
            new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration
                })
            },
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
        );
        
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(result);
    }
})
.WithName("ReadinessHealthCheck")
.WithDisplayName("Readiness Health Check")
.WithTags("Health");

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
})
.WithName("LivenessHealthCheck")
.WithDisplayName("Liveness Health Check")
.WithTags("Health");

// Add Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint("/metrics");

// Reverse proxy
app.MapReverseProxy();

app.Run();