# Use this to configure initial database migrations
# Make sure you run this from the project directory that contains BookingService.csproj

# For development environment (SQLite)
dotnet ef migrations add InitialCreate --context BookingDbContext --output-dir Domain/Migrations/Sqlite --configuration Development

# For production environment (PostgreSQL)
# dotnet ef migrations add InitialCreate --context BookingDbContext --output-dir Domain/Migrations/Postgres --configuration Production

# Apply migrations for development
dotnet ef database update --context BookingDbContext --configuration Development

# Apply migrations for production
# dotnet ef database update --context BookingDbContext --configuration Production
