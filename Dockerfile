# Multi-stage Dockerfile for Maliev Customer Service
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Maliev.CustomerService.sln", "."]
COPY ["Maliev.CustomerService.Api/Maliev.CustomerService.Api.csproj", "Maliev.CustomerService.Api/"]
COPY ["Maliev.CustomerService.Data/Maliev.CustomerService.Data.csproj", "Maliev.CustomerService.Data/"]
COPY ["Maliev.CustomerService.Tests/Maliev.CustomerService.Tests.csproj", "Maliev.CustomerService.Tests/"]

# Restore dependencies
RUN dotnet restore "Maliev.CustomerService.sln"

# Copy all source files
COPY . .

# Build the API project
WORKDIR "/src/Maliev.CustomerService.Api"
RUN dotnet build "Maliev.CustomerService.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Maliev.CustomerService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user (appuser with UID 1000)
RUN groupadd -r appuser --gid=1000 && \
    useradd -r -g appuser --uid=1000 --home-dir=/app --shell=/bin/bash appuser && \
    chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Copy published application from build stage
COPY --from=publish --chown=appuser:appuser /app/publish .

# Expose port 8080 (non-root user cannot bind to port 80)
EXPOSE 8080

# Set environment variables for ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/customers/liveness || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Maliev.CustomerService.Api.dll"]
