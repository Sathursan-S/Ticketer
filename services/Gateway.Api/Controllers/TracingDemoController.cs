using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Gateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TracingDemoController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TracingDemoController> _logger;
    private static readonly ActivitySource ActivitySource = new("Gateway.Api.Demo");

    public TracingDemoController(IHttpClientFactory httpClientFactory, ILogger<TracingDemoController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("trace-demo")]
    public async Task<IActionResult> TracingDemo()
    {
        using var activity = ActivitySource.StartActivity("tracing-demo");
        activity?.SetTag("demo.type", "distributed-tracing");
        activity?.SetTag("demo.version", "1.0");

        _logger.LogInformation("Starting distributed tracing demo");

        var results = new List<object>();

        // Simulate calls to different services
        var httpClient = _httpClientFactory.CreateClient();

        // Call to ticket service (if available)
        try
        {
            using var ticketActivity = ActivitySource.StartActivity("call-ticket-service");
            ticketActivity?.SetTag("service.name", "ticketservice");
            
            var ticketResponse = await httpClient.GetAsync("http://ticketservice:8080/health");
            results.Add(new { Service = "TicketService", Status = ticketResponse.StatusCode.ToString() });
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            results.Add(new { Service = "TicketService", Status = "Error", Message = ex.Message });
        }

        // Call to booking service (if available)
        try
        {
            using var bookingActivity = ActivitySource.StartActivity("call-booking-service");
            bookingActivity?.SetTag("service.name", "bookingservice");
            
            var bookingResponse = await httpClient.GetAsync("http://bookingservice:80/health");
            results.Add(new { Service = "BookingService", Status = bookingResponse.StatusCode.ToString() });
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            results.Add(new { Service = "BookingService", Status = "Error", Message = ex.Message });
        }

        activity?.SetTag("demo.services_called", results.Count);
        _logger.LogInformation("Completed distributed tracing demo with {ServiceCount} services", results.Count);

        return Ok(new 
        { 
            Message = "Distributed tracing demo completed", 
            TraceId = Activity.Current?.TraceId.ToString(),
            SpanId = Activity.Current?.SpanId.ToString(),
            Results = results,
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}