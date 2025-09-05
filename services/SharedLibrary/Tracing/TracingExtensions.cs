using System.Diagnostics;
using MassTransit;

namespace SharedLibrary.Tracing;

public static class TracingExtensions
{
    public static readonly ActivitySource ActivitySource = new("Ticketer.Saga");
    
    /// <summary>
    /// Creates a new activity for saga state transitions
    /// </summary>
    public static Activity? StartSagaActivity<T>(this ConsumeContext<T> context, string operationName, BookingStateMachineInstance? saga = null) 
        where T : class
    {
        var activity = ActivitySource.StartActivity($"saga.{operationName}");
        
        if (activity != null)
        {
            activity.SetTag("saga.correlation_id", context.CorrelationId?.ToString());
            activity.SetTag("saga.operation", operationName);
            activity.SetTag("message.type", context.Message?.GetType().Name);
            
            if (saga != null)
            {
                activity.SetTag("saga.id", saga.CorrelationId.ToString());
                activity.SetTag("saga.state", saga.CurrentState);
            }
            
            // Add trace context from message headers if available
            if (context.Headers.TryGetHeader("traceparent", out var traceParent))
            {
                activity.SetTag("trace.parent", traceParent.ToString());
            }
        }
        
        return activity;
    }
    
    /// <summary>
    /// Creates a new activity for message publishing with trace context propagation
    /// </summary>
    public static Activity? StartPublishActivity<T>(this ConsumeContext<T> context, string messageName)
        where T : class
    {
        var activity = ActivitySource.StartActivity($"saga.publish.{messageName}");
        
        if (activity != null)
        {
            activity.SetTag("saga.correlation_id", context.CorrelationId?.ToString());
            activity.SetTag("message.name", messageName);
            activity.SetTag("message.direction", "outbound");
        }
        
        return activity;
    }
    
    /// <summary>
    /// Adds trace context to message headers for propagation
    /// </summary>
    public static void AddTraceContext<T>(this PublishContext<T> context) 
        where T : class
    {
        var currentActivity = Activity.Current;
        if (currentActivity != null)
        {
            context.Headers.Set("traceparent", currentActivity.Id);
            context.Headers.Set("tracestate", currentActivity.TraceStateString);
        }
    }
    
    /// <summary>
    /// Records saga state transition
    /// </summary>
    public static void RecordStateTransition(this Activity activity, string fromState, string toState)
    {
        activity?.SetTag("saga.state.from", fromState);
        activity?.SetTag("saga.state.to", toState);
        activity?.AddEvent(new ActivityEvent($"State transition: {fromState} -> {toState}"));
    }
    
    /// <summary>
    /// Records saga failure
    /// </summary>
    public static void RecordSagaFailure(this Activity activity, string reason, Exception? exception = null)
    {
        activity?.SetTag("saga.failed", true);
        activity?.SetTag("saga.failure.reason", reason);
        
        if (exception != null)
        {
            activity?.SetTag("error.type", exception.GetType().Name);
            activity?.SetTag("error.message", exception.Message);
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        }
        
        activity?.AddEvent(new ActivityEvent($"Saga failed: {reason}"));
    }
}

/// <summary>
/// Interface for saga state machine instances to support tracing
/// </summary>
public interface ITracingSagaStateMachineInstance
{
    Guid CorrelationId { get; set; }
    string? CurrentState { get; set; }
}

/// <summary>
/// Marker interface for booking state machine instances
/// </summary>
public interface BookingStateMachineInstance : ITracingSagaStateMachineInstance
{
    Guid BookingId { get; set; }
    string? CustomerId { get; set; }
}