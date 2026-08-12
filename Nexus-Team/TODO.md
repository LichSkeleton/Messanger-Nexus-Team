# Technical TODOs

## Milestone: Make WebSocket payload serialization source-generation safe

**Status:** Deferred
**Recorded:** 2026-08-12
**Priority:** High before the next WebSocket feature release

### Problem

`JsonSerializerOptionsFactory.WebSocket` uses
`NexusTeamJsonSerializerContext` as its only `TypeInfoResolver`. This is fast and
type-safe for registered DTOs, but runtime serialization fails with
`NotSupportedException` when a payload type is not registered in the generated
context.

The application currently passes anonymous or client-private payload types to
these serializer options. Known affected locations include:

- `src/NexusTeam.Server/Controllers/ChatsController.cs`: anonymous chat-deleted payload.
- `src/NexusTeam.Server/Middleware/WebSocketHandler.cs`: anonymous rate-limit error payload.
- `src/NexusTeam.Client/Services/MessagingService.cs`: anonymous typing-indicator payload.
- `src/NexusTeam.Client/Services/MessagingService.cs`: private
  `TypingIndicatorPayload` and `ChatDeletedPayload` deserialization types.

The regression test
`WebSocket_WhenSerializingAnonymousPayload_SupportsControllerPayloads` documents
the issue in
`tests/unit/NexusTeam.Shared.Tests/Serialization/JsonSerializerOptionsFactoryTests.cs`.
It is intentionally failing while this milestone is deferred.

The E2E regression `WS-11 Message rate limiting returns an explicit error`
also demonstrates the user-visible impact: the rate limit is enforced, but the
anonymous error payload cannot be serialized and the client receives a generic
`Internal server error` envelope instead of the rate-limit response.

### Recommended solution

Keep source-generated serialization strict. Introduce explicit shared DTOs for
every WebSocket payload currently represented by an anonymous or private type,
register those DTOs with `NexusTeamJsonSerializerContext`, and update the client
and server call sites to use them.

Avoid solving this by adding reflection fallback unless supporting arbitrary
runtime payload types becomes an explicit requirement. Reflection fallback is
quicker, but it weakens compile-time coverage of the wire contract and may make
Native AOT or trimming issues harder to detect.

### Acceptance criteria

- No anonymous or private payload type is serialized or deserialized with
  `JsonSerializerOptionsFactory.WebSocket`.
- Every WebSocket payload DTO is declared in the shared project and registered
  with `NexusTeamJsonSerializerContext`.
- Chat deletion, typing indicator, and rate-limit error payloads serialize and
  deserialize using camelCase property names.
- Round-trip tests cover each newly introduced payload DTO.
- `docker compose run --rm --build unit-test` completes with zero failed tests.

## Milestone: Make participant validation null-safe

**Status:** Deferred
**Recorded:** 2026-08-12
**Priority:** High before relying on automatic API request validation

### Problem

`CreateChatRequestValidator` applies both `NotNull()` and
`Must(x => x.Count >= 1)` to `ParticipantIds`. FluentValidation continues to
the predicate after the null check, so a JSON request containing
`"participantIds": null` causes a `NullReferenceException` instead of a normal
validation failure.

The regression test
`Validate_WithNullParticipants_HasRequiredError` documents the issue in
`tests/unit/NexusTeam.Server.Tests/Validators/CreateChatRequestValidatorTests.cs`.
It is intentionally failing while this milestone is deferred.

The E2E regression `API-04 Null participant list is a validation error, not
server error` confirms that the current public API returns HTTP 500 for this
request instead of HTTP 400.

### Recommended solution

Stop the rule chain after `NotNull()` by using cascade-stop behavior, or make
the predicate null-safe. Preserve the separate validation messages for a null
list and an empty list.

### Acceptance criteria

- A null participant list returns the message `Participant list is required`.
- An empty participant list returns the minimum-participant message.
- Neither case throws an exception from the validator.
- The regression test passes without weakening its assertion.

## Milestone: Complete and harden exception-to-HTTP mapping

**Status:** Deferred
**Recorded:** 2026-08-12
**Priority:** High before exposing the API outside a trusted environment

### Problem

`ExceptionHandlingMiddleware` does not map all existing domain exception types.
`UnauthorizedException` currently becomes HTTP 500 instead of 401, and
`NotFoundException` becomes HTTP 500 instead of 404.

For unexpected exceptions, the middleware returns a generic public `Message`
but still copies `exception.Message` into the JSON `Detail` field. This can leak
database, infrastructure, or credential-related information to clients.

The following regression tests document the issues in
`tests/unit/NexusTeam.Server.Tests/Middleware/ExceptionHandlingMiddlewareTests.cs`:

- `InvokeAsync_WithUnauthorizedException_ReturnsUnauthorized`
- `InvokeAsync_WithNotFoundException_ReturnsNotFound`
- `InvokeAsync_WithUnexpectedException_DoesNotExposeInternalDetail`

They are intentionally failing while this milestone is deferred.

### Recommended solution

Add explicit HTTP mappings for every domain exception used by the application.
Return diagnostic details only in development, or remove the public `Detail`
field for unexpected errors. Keep the full exception in structured server logs.

### Acceptance criteria

- `UnauthorizedException` returns HTTP 401.
- `NotFoundException` returns HTTP 404.
- Unexpected exceptions return HTTP 500 without exposing their internal message.
- Known validation and conflict mappings remain unchanged.
- All exception middleware regression tests pass.

The E2E regression `FOLDER-02 User cannot read another user's folder` confirms
the same missing mapping at the HTTP boundary: the folder service rejects the
request with `UnauthorizedException`, but middleware exposes it as HTTP 500.

## Milestone: Handle malformed stored password hashes safely

**Status:** Deferred
**Recorded:** 2026-08-12
**Priority:** Medium before introducing password-hash migrations

### Problem

`BcryptPasswordHasher.VerifyPasswordAsync` calls `BCrypt.Verify` without
handling malformed hashes. If a stored password hash is corrupted or comes from
an unsupported legacy format, login throws instead of returning an ordinary
authentication failure. The shared password hasher already handles this case
by returning `false`, so the two implementations currently have inconsistent
behavior.

The regression test `VerifyPasswordAsync_WithMalformedHash_ReturnsFalse`
documents the issue in
`tests/unit/NexusTeam.Server.Tests/Services/BcryptPasswordHasherTests.cs`. It is
intentionally failing while this milestone is deferred.

### Recommended solution

Catch BCrypt hash parsing/verification exceptions inside the server hasher,
log safely if needed, and return `false`. Keep argument validation behavior
separate from malformed stored-data handling.

### Acceptance criteria

- A malformed stored hash returns `false` without throwing.
- Correct passwords still verify successfully.
- Incorrect passwords still return `false`.
- Work-factor configuration remains unchanged.

## Milestone: Implement actual background presence reconciliation

**Status:** Deferred
**Recorded:** 2026-08-12
**Priority:** Medium before relying on the hosted presence worker

### Problem

`PresenceTrackingService` is registered as a hosted service and wakes every 30
seconds, but `UpdateUserPresenceAsync` only creates a dependency-injection scope
and resolves `IUserRepository`. It does not read active WebSocket connections,
update user status, persist last-seen timestamps, or use its injected
`IWebSocketConnectionManager`.

The class currently consumes background resources without performing presence
reconciliation. Real connection bookkeeping is implemented separately in
`WebSocketConnectionManager`.

### Recommended solution

Define the intended background reconciliation contract first. Use active
connection snapshots to update online/offline state and last-seen timestamps,
and remove the worker entirely if all presence transitions are meant to remain
event-driven in `WebSocketHandler`.

### Acceptance criteria

- The hosted service either performs documented presence reconciliation or is
  removed from dependency injection.
- `IWebSocketConnectionManager` is no longer an unused dependency.
- Repository work has an observable purpose and is covered by tests.
- Cancellation stops the worker promptly without an extra retry delay.

## Milestone: Make WebSocket connection registration atomic

**Status:** Deferred
**Recorded:** 2026-08-12
**Priority:** High before supporting reconnect races or multiple server nodes

### Problem

`WebSocketConnectionManager.AddConnection` updates three separate indexes. If a
duplicate `connectionId` is registered for another user, `connections.TryAdd`
and `connectionToUser.TryAdd` keep the original mapping, but
`userToConnections.AddOrUpdate` still adds that ID to the second user's set.
The same connection can therefore appear in two user broadcast lists while its
authoritative reverse mapping points to only one user.

The regression test
`AddConnection_WithDuplicateId_DoesNotAssociateItWithSecondUser` documents the
issue in
`tests/unit/NexusTeam.Server.Tests/Services/WebSocketConnectionManagerTests.cs`. It
is intentionally failing while this milestone is deferred.

### Recommended solution

Treat registration as one atomic logical operation. Reject a duplicate
connection ID before updating any secondary index, or explicitly remove and
replace every old mapping under a consistent synchronization strategy.

### Acceptance criteria

- A connection ID belongs to exactly one user and one socket at any time.
- Duplicate registration cannot add the ID to a second user's connection set.
- Removal clears all indexes without leaving stale user mappings.
- Concurrent add/remove tests remain stable.
- Broadcast never sends another user's connection through a stale index.

## Milestone: Enforce resource-level authorization consistently

**Status:** Deferred
**Recorded:** 2026-08-12
**Priority:** Critical before exposing the API outside a trusted environment

### Problem

Several endpoints authenticate selected operations but do not verify that the
caller may access the requested resource. The isolated E2E suite documents the
current gaps:

- `SEC-06`: a non-participant can read private chat metadata.
- `SEC-07`: a non-participant can read private message history.
- `SEC-11`: anonymous callers can read message history.
- `SEC-12`: anonymous callers can list message attachments.
- `SEC-13`: any group member can delete the entire owner-created group.
- `SEC-15` and `SEC-16`: generated-image metadata is readable anonymously and
  by another authenticated user.
- `SEC-17` and `SEC-18`: attachment upload/delete endpoints do not require an
  authenticated caller.
- `SEC-20`: a non-participant can react to a private message.

Message sending already rejects non-participants (`SEC-19` passes), showing
that participant checks exist but are not applied consistently across reads,
reactions, attachments and destructive operations.

### Recommended solution

Introduce one reusable authorization policy/service for chat membership and
resource ownership. Apply it before repository or filesystem work in every
chat, message, attachment and generated-image endpoint. Define explicitly
whether group deletion is owner-only; the current E2E security contract assumes
that only the group owner may delete the entire group.

### Acceptance criteria

- Anonymous callers cannot read or mutate chat-owned resources.
- Authenticated non-participants cannot read chat metadata or message history.
- Attachment access is derived from the owning message and chat membership.
- Generated-image metadata and bytes are visible only to their owner.
- Reactions require chat participation.
- Whole-group deletion requires owner authority.
- `SEC-06`, `SEC-07`, `SEC-11` through `SEC-13`, and `SEC-15` through `SEC-20`
  pass in the isolated E2E suite.

## Milestone: Suppress server implementation disclosure

**Status:** Deferred
**Recorded:** 2026-08-12
**Priority:** Medium before public deployment

### Problem

Security headers such as `X-Content-Type-Options`, `X-Frame-Options` and
`Referrer-Policy` are present, but Kestrel still emits `Server: Kestrel`.
`SEC-10 Security headers are present on API responses` documents this leak.

### Recommended solution

Disable Kestrel's server header through `KestrelServerOptions.AddServerHeader`
and preserve the existing security-header middleware.

### Acceptance criteria

- API responses do not disclose the server implementation.
- Existing defensive response headers remain present.
- `SEC-10` passes in the isolated E2E suite.
