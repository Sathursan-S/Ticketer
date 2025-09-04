using BookingService.Application.Sagas;
using BookingService.HealthChecks;
using BookingService.Infrastructure.Messaging;
using BookingService.Infrastructure.Persistence.Saga;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
using System.Diagnostics;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Database --------------------
builder.Services.AddDbContext<BookingSagaDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresSQL"), npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "saga");
    });
});

// -------------------- RabbitMQ Config --------------------
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<BookingStateMachine, BookingState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.DatabaseFactory(provider => () =>
                provider.GetRequiredService<BookingSagaDbContext>());
            r.UsePostgres();
        });

    x.SetKebabCaseEndpointNameFormatter();
    x.AddSagas(typeof(Program).Assembly);
    x.AddConsumers(typeof(Program).Assembly);

    x.UsingRabbitMq((context, cfg) =>
    {
        var mq = context.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
        var vhost = string.IsNullOrWhiteSpace(mq.VirtualHost) ? "/" : mq.VirtualHost;

        cfg.Host(mq.Host, vhost, h =>
        {
            h.Username(mq.Username);
            h.Password(mq.Password);
        });

        // Use raw JSON serialization
        cfg.ClearSerialization();
        cfg.UseRawJsonSerializer();
        cfg.UseRawJsonDeserializer();

        // Resilience
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        cfg.UseCircuitBreaker(cb =>
        {
            cb.TripThreshold = (int)0.15;
            cb.ActiveThreshold = 10;
            cb.ResetInterval = TimeSpan.FromMinutes(1);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// -------------------- Services --------------------
builder.Services.AddScoped<IPaymentPublisher, PaymentPublisher>();

builder.Services.AddSingleton(sp =>
{
    var mq = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    var logger = sp.GetRequiredService<ILogger<RabbitMqHealthCheck>>();
    return new RabbitMqHealthCheck(logger, mq.Host, (int)mq.Port);
});

// -------------------- Health Checks --------------------
builder.Services.AddHealthChecks()
    .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: new[] { "services", "messaging" })
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// -------------------- CORS --------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        policy.WithOrigins(corsOrigins ?? Array.Empty<string>())
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "BookingService")
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
        .AddSource("MassTransit")
        .AddSource("BookingService.*")
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
        .AddMeter("BookingService.*")
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

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Instance = context.HttpContext.Request.Path,
                Status = StatusCodes.Status400BadRequest,
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                Detail = "Please refer to the errors property for additional details."
            };

            return new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json", "application/problem+xml" }
            };
        };
    });

// -------------------- Swagger --------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = builder.Configuration["Swagger:ApiTitle"] ?? "BookingService API",
        Version = builder.Configuration["Swagger:ApiVersion"] ?? "v1",
        Description = builder.Configuration["Swagger:ApiDescription"] ?? "Enterprise-level API for booking management",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@bookingservice.com"
        },
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// -------------------- Build App --------------------
var app = builder.Build();

// Initialize the database asynchronously
_ = Task.Run(async () =>
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<BookingSagaDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    const int maxRetries = 10;
    const int delaySeconds = 5;
    
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogInformation("Attempting to initialize database (attempt {Attempt}/{MaxRetries})...", i + 1, maxRetries);
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Database initialization completed successfully.");
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database initialization failed (attempt {Attempt}/{MaxRetries}): {Message}", i + 1, maxRetries, ex.Message);
            
            if (i < maxRetries - 1)
            {
                logger.LogInformation("Retrying database initialization in {DelaySeconds} seconds...", delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }
    
    logger.LogError("Failed to initialize database after {MaxRetries} attempts. Application may not function correctly.", maxRetries);
});

// -------------------- Health Endpoints --------------------
app.MapHealthChecks("/health/rabbitmq", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = hc => hc.Tags.Contains("messaging"),
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

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
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

// -------------------- Middleware --------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Booking Service API v1");
        c.RoutePrefix = string.Empty;
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        c.DefaultModelsExpandDepth(-1);
    });
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Booking Service API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");
app.UseAuthorization();
app.MapControllers();

// Kubernetes readiness/liveness
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// Add Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint("/metrics");

await app.RunAsync();
