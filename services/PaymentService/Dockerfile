# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project files first for layer caching
COPY ["services/PaymentService/PaymentService.csproj", "services/PaymentService/"]
COPY ["services/SharedLibrary/SharedLibrary.csproj", "services/SharedLibrary/"]

# Restore dependencies
RUN dotnet restore "services/PaymentService/PaymentService.csproj"

# Copy the rest of the source code
COPY ["services/PaymentService/", "services/PaymentService/"]
COPY ["services/SharedLibrary/", "services/SharedLibrary/"]

# Build the application
WORKDIR "/src/services/PaymentService"
RUN dotnet build "PaymentService.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
WORKDIR "/src/services/PaymentService"
RUN dotnet publish "PaymentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8090
EXPOSE 443

# Copy the published files from the publish stage
COPY --from=publish /app/publish .

# Set the entry point
ENTRYPOINT ["dotnet", "PaymentService.dll"]
