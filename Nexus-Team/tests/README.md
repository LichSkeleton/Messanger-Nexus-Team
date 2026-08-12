# Test suites

The test tree is separated by test scope so each suite can have an independent
Docker image, Compose profile, dependencies, and execution lifecycle.

## Unit tests

Unit tests live under `tests/unit`. They do not require the application stack,
MongoDB, Oracle, or Redis to be running.

Run the complete unit suite with:

```sh
docker compose run --rm --build unit-test
```

To run only the green CI baseline or only documented regressions:

```sh
docker compose run --rm --build -e TEST_FILTER="Category!=Regression" unit-test
docker compose run --rm --build -e TEST_FILTER="Category=Regression" unit-test
```

The Compose service and profile are both named `unit-test`. Because the service
is profile-gated, a normal `docker compose up` does not build or run it.

## End-to-end tests

End-to-end tests live under `tests/e2e`. They use real HTTP and WebSocket
connections against the complete backend stack, including Oracle, MongoDB and
Redis.

Run them with:

```sh
docker compose --profile e2e-test run --rm --build e2e-test
```

The same `TEST_FILTER` values select the E2E baseline or documented regressions.

The Compose service and profile are both named `e2e-test`. The profile starts
isolated `oracle-e2e`, `mongo-e2e`, `redis-e2e`, `db-seeder-e2e` and
`server-e2e` services. They use dedicated credentials, database names and
volumes, so E2E execution never reads or mutates the regular application data.
The test process waits for the isolated API health check before starting.
