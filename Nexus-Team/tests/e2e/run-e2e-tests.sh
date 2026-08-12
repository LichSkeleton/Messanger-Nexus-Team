#!/bin/sh

dotnet test tests/e2e/NexusTeam.E2E.Tests/NexusTeam.E2E.Tests.csproj \
  --configuration Release \
  --no-restore \
  --logger "console;verbosity=normal"
