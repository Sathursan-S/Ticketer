using BookingService.Application.Sagas;
using BookingService.HealthChecks;
using BookingService.Infrastructure.Messaging;
using BookingService.Infrastructure.Persistence.Saga;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
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


builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));


builder.Services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<BookingStateMachine, BookingState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.DatabaseFactory(() => new BookingSagaDbContext(
                new DbContextOptionsBuilder<BookingSagaDbContext>()
                    .UseNpgsql(builder.Configuration.GetConnectionString("PostgresSQL"))
                    .Options));
            r.UsePostgres();
        });

    x.SetKebabCaseEndpointNameFormatter();
    x.AddSagas(typeof(Program).Assembly);
    x.AddConsumers(typeof(Program).Assembly);

    // x.UsingRabbitMq((context, cfg) =>
    // {
    //     var mq = context.GetRequiredService<IOptions<RabbitMqSettings>>().Value;

    //     cfg.Host(mq.Host, mq.Port, string.IsNullOrWhiteSpace(mq.VirtualHost) ? "/" : mq.VirtualHost, h =>
    //     {
    //         h.Username(mq.Username);
    //         h.Password(mq.Password);
    //     });

    //     cfg.ClearSerialization();
    //     cfg.UseRawJsonSerializer();
    //     cfg.UseRawJsonDeserializer();

    //     cfg.ConfigureEndpoints(context);
    // });
    x.UsingRabbitMq((context, cfg) =>
{
    var mq = context.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    var vhost = string.IsNullOrWhiteSpace(mq.VirtualHost) ? "/" : mq.VirtualHost;

    cfg.Host(mq.Host, vhost, h =>
    {
        h.Username(mq.Username);
        h.Password(mq.Password);
    });

    cfg.ClearSerialization();
    cfg.UseRawJsonSerializer();
    cfg.UseRawJsonDeserializer();

    cfg.ConfigureEndpoints(context);
});

});

builder.Services.AddScoped<IPaymentPublisher, PaymentPublisher>();

builder.Services.AddSingleton(sp =>
{
    var mq = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    var logger = sp.GetRequiredService<ILogger<RabbitMqHealthCheck>>();
    return new RabbitMqHealthCheck(logger, mq.Host, mq.Port);
});

builder.Services.AddHealthChecks()
    .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: new[] { "services", "messaging" });

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
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<BookingSagaDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Ensuring database is created...");
        await ctx.Database.EnsureCreatedAsync();
        logger.LogInformation("Database initialization completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database: {Message}", ex.Message);
        throw new InvalidOperationException("Failed to initialize the database. See inner exception for details.", ex);
    }
}

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

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Booking Service API v1");
    c.RoutePrefix = string.Empty; 
});

app.UseCors("AllowSpecificOrigins");
app.MapControllers();

await app.RunAsync();