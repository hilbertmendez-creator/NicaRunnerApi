# Session Revocation Specification

## Purpose

Ensure a deactivated user's already-issued access token stops granting access
promptly, instead of remaining valid for up to its full lifetime
(60 min, `JwtSettings.cs:8`). Complements, and does not replace, the existing
login/refresh `IsActive` checks.

## Existing Behavior (context — not modified by this change)

- Login and Google-login reject inactive users at authentication time
  (`AuthService.cs:55,123,136`).
- Refresh is refused once `IsActive: false` (`RefreshTokenService.cs:40`).
- Access tokens are stateless JWTs with `ValidateLifetime` only; no revocation list
  exists (`Program.cs:220-237`).
- Web and mobile already handle 401 identically: web retries once via
  `/auth/refresh`, then clears the session on failure (`client.ts:91-128`); mobile
  does the equivalent in `TokenAuthenticator.kt:40-87`. No client code changes are
  required by this capability.

## Requirements

### Requirement: Per-request active-status check

When enabled by configuration, an authenticated request carrying a validated JWT for
a user whose `IsActive` is false MUST be rejected with 401, enforced via
`OnTokenValidated` on the existing `JwtBearerEvents` (`Program.cs:246`).

#### Scenario: Disabled user's next request

- GIVEN a user is deactivated mid-session and the config flag is enabled
- WHEN that user's next authenticated request arrives with a still-valid JWT
- THEN the API rejects it with 401
- AND the client's existing refresh flow runs, refresh is refused
  (`IsActive: false`), and the client clears the session

#### Scenario: Active user unaffected

- GIVEN a valid, active user
- WHEN they make an authenticated request
- THEN the request MUST NOT be rejected by this check (verified by integration test)

### Requirement: Configuration flag gate

The per-request check MUST run only when a configuration flag is enabled, so it can
be switched off in production without a redeploy.

#### Scenario: Flag disabled

- GIVEN the flag is off
- WHEN a disabled user makes a request with a still-valid JWT
- THEN the request is not rejected by this check (falls back to the pre-existing
  behavior: valid until token expiry)

### Requirement: Fail-open on infrastructure error

If the cache or the database is unreachable while the check is evaluated, the
request MUST proceed (fail open) rather than being rejected.

#### Scenario: Cache unavailable

- GIVEN the flag is enabled and the cache backing the check throws or times out
- WHEN a request is evaluated
- THEN the request is not rejected by this check alone

#### Scenario: Database unavailable on cache miss

- GIVEN the flag is enabled, the cache has no entry, and the DB lookup fails
- WHEN a request is evaluated
- THEN the request is not rejected by this check alone

### Requirement: Cache invalidation on deactivation

`UserManagementService.UpdateAsync` MUST invalidate the cached `IsActive` entry for
the target user at the moment `IsActive` is reassigned (near `UpdateAsync:105-109`),
through an Application-layer abstraction that does not reference
`Microsoft.Extensions.Caching` directly.

#### Scenario: Explicit invalidation on deactivation

- GIVEN an admin deactivates a user
- WHEN `UpdateAsync` persists `IsActive: false`
- THEN the cached entry for that user is invalidated in the same operation, ahead
  of relying on TTL expiry

## Not Guaranteed

- Short-TTL window: between deactivation and invalidation propagating, a disabled
  user's request MAY still succeed for a bounded number of seconds. Accepted
  staleness, not a defect.
- An already-open SignalR connection for a disabled Admin/Lector is NOT forcibly
  aborted; it is allowed to finish, and the user is refused only at reconnect
  (`RaceDashboardHub.cs:14` is `Administrador`/`Lector` only — a Capturista never
  holds a hub connection, so this has no overlap with the in-flight-Capturista
  warning in `user-status-management`).
