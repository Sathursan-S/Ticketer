using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Domain.DbContexts;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options)
    {
    }

    public DbSet<Booking> Bookings { get; set; }
}
