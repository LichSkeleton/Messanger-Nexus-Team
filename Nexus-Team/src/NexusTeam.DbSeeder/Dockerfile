# ============================================================================
# Stage 1: Build
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY Nexus-Team.sln ./
COPY Directory.Build.props ./

# Copy project file only (for better layer caching)
COPY src/NexusTeam.DbSeeder/NexusTeam.DbSeeder.csproj ./src/NexusTeam.DbSeeder/

# Restore dependencies
RUN dotnet restore src/NexusTeam.DbSeeder/NexusTeam.DbSeeder.csproj

# Copy source code
COPY src/NexusTeam.DbSeeder/ ./src/NexusTeam.DbSeeder/

# Build application
WORKDIR /src/src/NexusTeam.DbSeeder
RUN dotnet build NexusTeam.DbSeeder.csproj -c Release -o /app/build

# Publish application
RUN dotnet publish NexusTeam.DbSeeder.csproj -c Release -o /app/publish /p:UseAppHost=false

# ============================================================================
# Stage 2: Runtime
# ============================================================================
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Create directory for configuration files (will be mounted as volume)
RUN mkdir -p /app/configs

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_EnableDiagnostics=0

# Run seeder
ENTRYPOINT ["dotnet", "NexusTeam.DbSeeder.dll"]
