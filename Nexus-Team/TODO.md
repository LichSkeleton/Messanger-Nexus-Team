# Technical TODOs

## Milestone: Make WebSocket payload serialization source-generation safe

**Status:** Done
**Recorded:** 2026-08-12
**Completed:** 2026-08-12
**Priority:** High before the next WebSocket feature release

Introduced shared `ChatDeletedPayload`, `TypingIndicatorPayload`, and
`RateLimitErrorPayload` DTOs, registered them with
`NexusTeamJsonSerializerContext`, and updated server/client call sites. Round-trip
unit tests cover the new payloads.

## Milestone: Make participant validation null-safe

**Status:** Done
**Recorded:** 2026-08-12
**Completed:** 2026-08-12
**Priority:** High before relying on automatic API request validation

`CreateChatRequestValidator` now uses `CascadeMode.Stop` after `NotNull()` so a
null participant list returns a validation error instead of throwing.

## Milestone: Complete and harden exception-to-HTTP mapping

**Status:** Done
**Recorded:** 2026-08-12
**Completed:** 2026-08-12
**Priority:** High before exposing the API outside a trusted environment

`ExceptionHandlingMiddleware` maps `UnauthorizedException` → 401 and
`NotFoundException` → 404. Unexpected errors no longer expose internal
`exception.Message` in the public `Detail` field. Folder reads for non-owners
return null (HTTP 404) instead of leaking ownership via 401.

## Milestone: Handle malformed stored password hashes safely

**Status:** Done
**Recorded:** 2026-08-12
**Completed:** 2026-08-12
**Priority:** Medium before introducing password-hash migrations

`BcryptPasswordHasher.VerifyPasswordAsync` catches hash parse/verify failures and
returns `false`, matching the shared hasher behavior.

## Milestone: Implement actual background presence reconciliation

**Status:** Done
**Recorded:** 2026-08-12
**Completed:** 2026-08-12
**Priority:** Medium before relying on the hosted presence worker

`PresenceTrackingService` now snapshots connected users via
`IWebSocketConnectionManager.GetConnectedUserIds()`, refreshes `LastSeenAt`, and
restores Online status when a connected user was marked Offline. Cancellation
exits without waiting through the error retry delay.

## Milestone: Make WebSocket connection registration atomic

**Status:** Done
**Recorded:** 2026-08-12
**Completed:** 2026-08-12
**Priority:** High before supporting reconnect races or multiple server nodes

`AddConnection` rejects duplicate connection IDs before updating secondary
indexes, so a connection ID cannot appear under a second user.

## Milestone: Enforce resource-level authorization consistently

**Status:** Done
**Recorded:** 2026-08-12
**Completed:** 2026-08-12
**Priority:** Critical before exposing the API outside a trusted environment

Added `IResourceAuthorizationService` and applied membership/ownership checks to
chat metadata, message history, reactions, attachments, generated images, and
group deletion (owner-only for non-DM chats). Anonymous callers are rejected with
401 on previously open read/mutate endpoints.

## Milestone: Suppress server implementation disclosure

**Status:** Done
**Recorded:** 2026-08-12
**Completed:** 2026-08-12
**Priority:** Medium before public deployment

Kestrel `AddServerHeader` is disabled in `Program.cs`. Existing security-header
middleware is unchanged.
