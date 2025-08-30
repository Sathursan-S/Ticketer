using BookingService.Application.Sagas;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Persistence.Saga;

public class BookingSagaMap : SagaClassMap<BookingState>
{
    protected override void Configure(EntityTypeBuilder<BookingState> entity, ModelBuilder model)
    {
        entity.HasKey(x=>x.CorrelationId);
        entity.Property(x => x.CurrentState);
        entity.Property(x => x.BookingId);
        entity.Property(x => x.CustomerId);
        entity.Property(x => x.EventId);
        entity.Property(x => x.NumberOfTickets);
        entity.Property(x => x.CreatedAt);

        entity.Property(x => x.TotalPrice);
        entity.Property(x => x.PaymentIntentId);

        // Persist Tickets as string (CSV)
        entity.Property(x => x.Tickets)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse).ToList()
            );
    }
}