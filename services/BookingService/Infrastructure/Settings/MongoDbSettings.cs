namespace BookingService.Infrastructure.Settings;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string BookingsCollectionName { get; set; } = null!;
}
