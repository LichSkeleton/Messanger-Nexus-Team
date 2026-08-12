#!/bin/sh

if [ -n "${TEST_FILTER:-}" ]; then
  set -- --filter "$TEST_FILTER"
fi

dotnet test tests/e2e/NexusTeam.E2E.Tests/NexusTeam.E2E.Tests.csproj \
  --configuration Release \
  --no-restore \
  --logger "console;verbosity=normal" \
  "$@"
