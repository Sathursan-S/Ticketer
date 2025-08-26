using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TicketService;
using TicketService.Extensions;
using TicketService.Mappers;
using TicketService.Repositoy;
using TicketService.Repository;
using TicketService.Services;

var builder = WebApplication.CreateBuilder(args);

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
                "http://localhost:4200"  // Angular dev server
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

// Add application services
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketService, TicketService.Services.TicketService>();

// Add health checks
builder.Services.AddHealthChecks(builder.Configuration);

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
app.UseHealthChecks();

await app.RunAsync();
