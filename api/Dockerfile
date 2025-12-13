FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["PhotobookAPI.csproj", "."]

# Restore dependencies
RUN dotnet restore "PhotobookAPI.csproj"

# Copy source code
COPY . .

# Build the application
RUN dotnet build "PhotobookAPI.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "PhotobookAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy from build stage
COPY --from=build /app/publish .

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Start application
ENTRYPOINT ["dotnet", "PhotobookAPI.dll"]
