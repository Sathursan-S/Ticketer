FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5300

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project files first for layer caching
COPY ["services/TicketService/TicketService.csproj", "services/TicketService/"]
COPY ["services/SharedLibrary/SharedLibrary.csproj", "services/SharedLibrary/"]

# Restore dependencies
RUN dotnet restore "services/TicketService/TicketService.csproj"

# Copy the rest of the source code
COPY ["services/TicketService/", "services/TicketService/"]
COPY ["services/SharedLibrary/", "services/SharedLibrary/"]

# Build the application
WORKDIR "/src/services/TicketService"
RUN dotnet build "TicketService.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/services/TicketService"
RUN dotnet publish "TicketService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TicketService.dll"]
