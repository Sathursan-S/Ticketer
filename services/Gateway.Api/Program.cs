using Microsoft.AspNetCore.HttpOverrides;
using Yarp.ReverseProxy;

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

// Swagger (for the gateway itself; optional)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// YARP from config
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway.Api v1");
    c.SwaggerEndpoint("/api/payments/swagger/v1/swagger.json", "Payment Service v1");
    c.SwaggerEndpoint("/api/auth/v3/api-docs", "Auth Service v1");
    c.SwaggerEndpoint("/api/booking/swagger/v1/swagger.json", "Booking Service v1");
    c.SwaggerEndpoint("/api/tickets/swagger/v1/swagger.json", "Ticket Service v1");
    c.RoutePrefix = "swagger"; // or "" to serve at the root
});



// Forwarded headers if behind proxies (optional but useful)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// CORS
app.UseCors("CorsPolicy");

// Health endpoint
app.MapGet("/", () => Results.Ok(new { status = "ok", service = "Gateway.Api" }));

// Reverse proxy
app.MapReverseProxy();

app.Run();