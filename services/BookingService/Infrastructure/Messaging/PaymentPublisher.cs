using MassTransit;
using System.Net.Mime;
using BookingService.Contracts.Public;

namespace BookingService.Infrastructure.Messaging;

public static class PaymentRouting
{
    public const string Exchange   = "payment-events";   // topic exchange
    public const string KeySuccess = "payment.succeeded";
    public const string KeyFailed  = "payment.failed";
}

public interface IPaymentPublisher
{
    Task PublishSuccessAsync(PaymentSucceeded msg, CancellationToken ct = default);
    Task PublishFailureAsync(PaymentFailed msg,  CancellationToken ct = default);
}

public sealed class PaymentPublisher : IPaymentPublisher
{
    private readonly ISendEndpointProvider _send;
    public PaymentPublisher(ISendEndpointProvider send) => _send = send;

    public async Task PublishSuccessAsync(PaymentSucceeded msg, CancellationToken ct = default)
    {
        var ep = await _send.GetSendEndpoint(new Uri($"exchange:{PaymentRouting.Exchange}"));
        await ep.Send(msg, ctx =>
        {
            ctx.Headers.Set("Content-Type", "application/json");
            ctx.SetRoutingKey(PaymentRouting.KeySuccess);
        }, ct);
    }

    public async Task PublishFailureAsync(PaymentFailed msg, CancellationToken ct = default)
    {
        var ep = await _send.GetSendEndpoint(new Uri($"exchange:{PaymentRouting.Exchange}"));
        await ep.Send(msg, ctx =>
        {
            ctx.Headers.Set("Content-Type", "application/json");
            ctx.SetRoutingKey(PaymentRouting.KeyFailed);
        }, ct);
    }
}