using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using BookingService.Application.Sagas;

namespace BookingService.Infrastructure.Persistence.Saga;

public class BookingSagaDbContext : SagaDbContext
{
    public BookingSagaDbContext(DbContextOptions<BookingSagaDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var sagaMap in Configurations)
        {
            sagaMap.Configure(modelBuilder);
        }
        
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
    
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new BookingSagaMap(); }
    }
}