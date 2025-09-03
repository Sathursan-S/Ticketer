FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
# Only expose HTTPS port if we're using HTTPS
EXPOSE 443
# For development purposes, we're not enforcing HTTPS
ENV ASPNETCORE_URLS=http://+:80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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