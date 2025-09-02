# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS=http://+:80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first (match your folder names)
COPY ["services/Gateway.Api/Gateway.Api.csproj", "services/Gateway.Api/"]
# If Gateway.Api references SharedLibrary, keep this next line; else you can remove it
COPY ["services/SharedLibrary/SharedLibrary.csproj", "services/SharedLibrary/"]

RUN dotnet restore "services/Gateway.Api/Gateway.Api.csproj"

# Copy the rest of the source
COPY ["services/Gateway.Api/", "services/Gateway.Api/"]
# If referenced
COPY ["services/SharedLibrary/", "services/SharedLibrary/"]

WORKDIR "/src/services/Gateway.Api"
RUN dotnet publish "Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
