using AutoMapper;
using BookingService.Domain.Mappings;
using BookingService.Domain.Settings;
using BookingService.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add MongoDB settings
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));

// Register MongoDB repository
builder.Services.AddSingleton<IBookingRepository, BookingRepositoryMongo>();
builder.Services.AddAutoMapper(typeof(MappingProfile));
// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        policy => 
        {
            var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            policy.WithOrigins(corsOrigins ?? Array.Empty<string>())
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization if needed
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Customize BadRequest responses when model validation fails
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
            // URL from configuration can be added here if needed
        },
        License = new OpenApiLicense
        {
            Name = "MIT License"
            // URL from configuration can be added here if needed
        }
    });
    
    // Set the comments path for the Swagger JSON and UI
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    // Security definitions can be added here when authentication is implemented
    // For now, we'll keep the API endpoints accessible without authentication
});

// Add repository and service registrations
builder.Services.AddScoped<BookingService.Repositories.IBookingRepository, BookingService.Repositories.BookingRepository>();
builder.Services.AddScoped<BookingService.Services.IBookingService, BookingService.Services.BookingService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookingService API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.EnableDeepLinking();
        c.DisplayRequestDuration();
    });
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Use CORS with the policy defined above
app.UseCors("AllowSpecificOrigins");

app.UseRouting();
// We'll add authentication later when it's implemented
app.MapControllers();

await app.RunAsync();

