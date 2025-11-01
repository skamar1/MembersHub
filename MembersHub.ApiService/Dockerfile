# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["MembersHub.ApiService/MembersHub.ApiService.csproj", "MembersHub.ApiService/"]
COPY ["MembersHub.Core/MembersHub.Core.csproj", "MembersHub.Core/"]
COPY ["MembersHub.Infrastructure/MembersHub.Infrastructure.csproj", "MembersHub.Infrastructure/"]
COPY ["MembersHub.Application/MembersHub.Application.csproj", "MembersHub.Application/"]
COPY ["MembersHub.Shared/MembersHub.Shared.csproj", "MembersHub.Shared/"]
COPY ["MembersHub.ServiceDefaults/MembersHub.ServiceDefaults.csproj", "MembersHub.ServiceDefaults/"]

RUN dotnet restore "MembersHub.ApiService/MembersHub.ApiService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/MembersHub.ApiService"
RUN dotnet build "MembersHub.ApiService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MembersHub.ApiService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 8080

# Install required dependencies for globalization
RUN apk add --no-cache icu-libs

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "MembersHub.ApiService.dll"]
