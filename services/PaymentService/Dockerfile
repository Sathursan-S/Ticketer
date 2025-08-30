# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the main project file first for restore
COPY ["PaymentService.csproj", "./"]
COPY ["../SharedLibrary/SharedLibrary.csproj", "../SharedLibrary/"]

# Restore dependencies
RUN dotnet restore "PaymentService.csproj"

# Copy the rest of the source code
COPY . .
COPY ["../SharedLibrary/", "../SharedLibrary/"]

# Build the application
RUN dotnet build "PaymentService.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "PaymentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Copy the published files from the publish stage
COPY --from=publish /app/publish .

# Set the entry point
ENTRYPOINT ["dotnet", "PaymentService.dll"]
