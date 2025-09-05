FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use dynamic port binding for better container orchestration
ENV ASPNETCORE_URLS=http://+:80;https://+:443
ENV ASPNETCORE_HTTP_PORTS=80
ENV ASPNETCORE_HTTPS_PORTS=443

# Enable health checks
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project files first for layer caching
COPY ["services/BookingService/BookingService.csproj", "services/BookingService/"]
COPY ["services/SharedLibrary/SharedLibrary.csproj", "services/SharedLibrary/"]

# Restore dependencies
RUN dotnet restore "services/BookingService/BookingService.csproj"

# Copy the rest of the source code
COPY ["services/BookingService/", "services/BookingService/"]
COPY ["services/SharedLibrary/", "services/SharedLibrary/"]

# Build the application
WORKDIR "/src/services/BookingService"
RUN dotnet build "BookingService.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/services/BookingService"
RUN dotnet publish "BookingService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingService.dll"]