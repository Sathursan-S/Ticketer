using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace BookingService.Infrastructure.Persistence.Saga;

public class BookingSagaDbContext : SagaDbContext
{
    public BookingSagaDbContext(DbContextOptions options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
    
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new BookingSagaMap(); }
    }
}