# Payment Service API

The Payment Service provides a comprehensive payment processing solution with support for multiple payment gateways through a pluggable interface architecture.

## Features

- **Multi-Gateway Support**: Extensible interface for integrating multiple payment providers
- **Stripe Integration**: Full Stripe payment processing including payments, refunds, and webhooks
- **Webhook Handling**: Secure webhook processing for payment status updates
- **Refund Support**: Complete refund processing capabilities
- **Error Handling**: Comprehensive error handling and logging

## Configuration

### Stripe Configuration

Add the following to your `appsettings.json`:

```json
{
  "Stripe": {
    "SecretKey": "sk_test_your_stripe_secret_key",
    "PublishableKey": "pk_test_your_stripe_publishable_key", 
    "WebhookSecret": "whsec_your_webhook_endpoint_secret"
  }
}
```

## API Endpoints

### Process Payment
```http
POST /api/payment/process
Content-Type: application/json

{
  "bookingId": "550e8400-e29b-41d4-a716-446655440000",
  "customerId": "cust_customer_id",
  "amount": 99.99,
  "paymentMethod": "pm_card_visa"
}
```

**Response:**
```json
{
  "paymentIntentId": "pi_stripe_payment_intent_id",
  "status": "succeeded",
  "amount": 99.99,
  "bookingId": "550e8400-e29b-41d4-a716-446655440000",
  "paymentMethod": "pm_card_visa",
  "customerId": "cust_customer_id",
  "isSuccess": true,
  "payedAt": "2024-01-15T10:30:00Z"
}
```

### Process Refund
```http
POST /api/payment/refund
Content-Type: application/json

{
  "paymentIntentId": "pi_stripe_payment_intent_id",
  "amount": 50.00,
  "reason": "requested_by_customer",
  "bookingId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response:**
```json
{
  "refundId": "re_stripe_refund_id",
  "paymentIntentId": "pi_stripe_payment_intent_id",
  "isSuccess": true,
  "amount": 50.00,
  "status": "succeeded",
  "bookingId": "550e8400-e29b-41d4-a716-446655440000",
  "refundedAt": "2024-01-15T11:00:00Z"
}
```

### Webhook Handler
```http
POST /api/payment/webhook/stripe
Content-Type: application/json
Stripe-Signature: stripe_webhook_signature

{
  "webhook_payload_from_stripe"
}
```

## Architecture

### Payment Gateway Interface

The `IPaymentGateway` interface provides a common contract for all payment providers:

```csharp
public interface IPaymentGateway
{
    Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto dto);
    Task<RefundResultDto> RefundPaymentAsync(RefundRequestDto dto);
    Task<bool> ProcessWebhookAsync(string payload, string signature);
    string GatewayName { get; }
}
```

### Adding New Payment Gateways

To add a new payment gateway:

1. Create a new class implementing `IPaymentGateway`
2. Register it in `Program.cs`: `builder.Services.AddScoped<IPaymentGateway, YourGateway>();`
3. Add configuration settings if needed

Example:
```csharp
public class PayPalPaymentGateway : IPaymentGateway
{
    public string GatewayName => "PayPal";
    
    public async Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto dto)
    {
        // PayPal-specific implementation
    }
    
    // ... other methods
}
```

## Webhook Security

The service verifies webhook signatures to ensure authenticity:

- **Stripe**: Uses `Stripe-Signature` header for webhook verification
- **Future Gateways**: Each gateway implements its own verification method

## Error Handling

All payment operations include comprehensive error handling:

- Network failures are retried where appropriate
- Invalid requests return detailed error messages
- All errors are logged for monitoring and debugging

## Testing

Use the test endpoints to verify the service:

```http
GET /api/test/gateway-info
GET /api/test/stripe-config
POST /api/test/simulate-payment
```

## Integration with Booking Service

The payment service integrates with the existing booking flow:

1. Booking service initiates payment processing
2. Payment service processes payment via configured gateway
3. Webhook notifications update payment status
4. Booking service receives payment confirmation

## Security Considerations

- All sensitive payment data is handled by the payment gateway (Stripe)
- API keys and secrets are stored in configuration
- Webhook signatures are verified to prevent tampering
- All payment operations are logged for audit trails