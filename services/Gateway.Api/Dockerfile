# Use the .NET 8 runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# Use the .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["services/Gateway.Api/Gateway.Api.csproj", "services/Gateway.Api/"]

# Restore packages
RUN dotnet restore "services/Gateway.Api/Gateway.Api.csproj"

# Copy source code
COPY services/Gateway.Api/ services/Gateway.Api/

# Build the application
WORKDIR "/src/services/Gateway.Api"
RUN dotnet build "Gateway.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Gateway.Api.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]