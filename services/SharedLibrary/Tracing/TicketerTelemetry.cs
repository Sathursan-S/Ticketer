using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SharedLibrary.Tracing;

public static class TicketerTelemetry
{
    public const string ServiceName = "Ticketer";
    public const string ServiceVersion = "1.0.0";
    
    // Activity Sources for different domains
    public static readonly ActivitySource SagaActivitySource = new("Ticketer.Saga");
    public static readonly ActivitySource PaymentActivitySource = new("Ticketer.Payment");
    public static readonly ActivitySource TicketActivitySource = new("Ticketer.Ticket");
    public static readonly ActivitySource BookingActivitySource = new("Ticketer.Booking");
    public static readonly ActivitySource EventActivitySource = new("Ticketer.Event");
    public static readonly ActivitySource AuthActivitySource = new("Ticketer.Auth");
    public static readonly ActivitySource NotificationActivitySource = new("Ticketer.Notification");
    
    // Meters for custom metrics
    public static readonly Meter SagaMeter = new("Ticketer.Saga.Metrics");
    public static readonly Meter BusinessMeter = new("Ticketer.Business.Metrics");
    
    // Saga metrics
    public static readonly Counter<long> SagaStartedCounter = SagaMeter.CreateCounter<long>(
        "saga.started",
        "Number of sagas started");
        
    public static readonly Counter<long> SagaCompletedCounter = SagaMeter.CreateCounter<long>(
        "saga.completed",
        "Number of sagas completed successfully");
        
    public static readonly Counter<long> SagaFailedCounter = SagaMeter.CreateCounter<long>(
        "saga.failed", 
        "Number of sagas that failed");
        
    public static readonly Histogram<double> SagaDurationHistogram = SagaMeter.CreateHistogram<double>(
        "saga.duration",
        "ms",
        "Duration of saga execution");
    
    // Business metrics
    public static readonly Counter<long> BookingsCreatedCounter = BusinessMeter.CreateCounter<long>(
        "bookings.created",
        "Number of bookings created");
        
    public static readonly Counter<long> PaymentsProcessedCounter = BusinessMeter.CreateCounter<long>(
        "payments.processed", 
        "Number of payments processed");
        
    public static readonly Counter<long> TicketsReservedCounter = BusinessMeter.CreateCounter<long>(
        "tickets.reserved",
        "Number of tickets reserved");
    
    /// <summary>
    /// Record saga metrics with common tags
    /// </summary>
    public static void RecordSagaMetrics(string operation, string sagaType, TimeSpan duration, bool success = true)
    {
        var tags = new TagList
        {
            {"saga.type", sagaType},
            {"saga.operation", operation},
            {"saga.success", success}
        };
        
        if (success)
        {
            SagaCompletedCounter.Add(1, tags);
        }
        else
        {
            SagaFailedCounter.Add(1, tags);
        }
        
        SagaDurationHistogram.Record(duration.TotalMilliseconds, tags);
    }
    
    /// <summary>
    /// Common activity tags for consistent tracing
    /// </summary>
    public static class CommonTags
    {
        public const string ServiceName = "service.name";
        public const string ServiceVersion = "service.version";
        public const string CorrelationId = "correlation.id";
        public const string UserId = "user.id";
        public const string BookingId = "booking.id";
        public const string EventId = "event.id";
        public const string PaymentId = "payment.id";
        public const string OperationType = "operation.type";
        public const string SagaType = "saga.type";
        public const string SagaState = "saga.state";
        public const string MessageType = "message.type";
    }
}