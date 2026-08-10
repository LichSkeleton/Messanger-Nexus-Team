# ============================================================================
# Stage 1: Build
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution + shared build props / analyzer config
COPY Nexus-Team.sln ./
COPY Directory.Build.props ./
COPY StyleCop.ruleset ./
COPY stylecop.json ./

# Copy project files first for better layer caching (server + its dependency)
COPY src/NexusTeam.Server/NexusTeam.Server.csproj ./src/NexusTeam.Server/
COPY src/NexusTeam.Shared/NexusTeam.Shared.csproj ./src/NexusTeam.Shared/

# Restore dependencies
RUN dotnet restore src/NexusTeam.Server/NexusTeam.Server.csproj

# Copy the rest of the source needed to build the server
COPY src/NexusTeam.Server/ ./src/NexusTeam.Server/
COPY src/NexusTeam.Shared/ ./src/NexusTeam.Shared/

# Publish
WORKDIR /src/src/NexusTeam.Server
RUN dotnet publish NexusTeam.Server.csproj -c Release -o /app/publish /p:UseAppHost=false

# ============================================================================
# Stage 2: Runtime (ASP.NET Core shared framework)
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl is used by the container health check
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Correct globalization (Oracle/date handling) and quieter diagnostics
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_EnableDiagnostics=0 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5251

# 127.0.0.1 rather than "localhost": Kestrel listens on IPv4 only, so a
# localhost that resolves to ::1 first would fail the check.
HEALTHCHECK --interval=15s --timeout=5s --start-period=40s --retries=10 \
    CMD curl -fsS http://127.0.0.1:5251/health || exit 1

# The server takes the listening port as its single CLI argument.
ENTRYPOINT ["dotnet", "nexus-team-server.dll", "5251"]
