#!/bin/sh

status=0

dotnet test tests/unit/NexusTeam.Server.Tests/NexusTeam.Server.Tests.csproj \
  --configuration Release \
  --no-restore \
  --settings coverlet.runsettings || status=$?

dotnet test tests/unit/NexusTeam.Shared.Tests/NexusTeam.Shared.Tests.csproj \
  --configuration Release \
  --no-restore \
  --settings coverlet.runsettings || status=$?

exit "$status"
