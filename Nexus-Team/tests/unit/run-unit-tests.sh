#!/bin/sh

status=0

if [ -n "${TEST_FILTER:-}" ]; then
  set -- --filter "$TEST_FILTER"
fi

# Compile Server tests first. If this fails, stop before Shared tests so CI
# cannot publish Shared's 113 results as the full unit suite.
dotnet build tests/unit/NexusTeam.Server.Tests/NexusTeam.Server.Tests.csproj \
  --configuration Release \
  --no-restore || exit $?

dotnet build tests/unit/NexusTeam.Shared.Tests/NexusTeam.Shared.Tests.csproj \
  --configuration Release \
  --no-restore || exit $?

dotnet test tests/unit/NexusTeam.Server.Tests/NexusTeam.Server.Tests.csproj \
  --configuration Release \
  --no-restore \
  --settings coverlet.runsettings \
  "$@" || status=$?

dotnet test tests/unit/NexusTeam.Shared.Tests/NexusTeam.Shared.Tests.csproj \
  --configuration Release \
  --no-restore \
  --settings coverlet.runsettings \
  "$@" || status=$?

exit "$status"
