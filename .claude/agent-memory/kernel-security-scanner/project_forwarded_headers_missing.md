---
name: project_forwarded_headers_missing
description: RESOLVED (MSG-K022 2026-04-07) — UseForwardedHeaders configured in Program.cs with KnownProxies=loopback and KnownNetworks cleared. Do not re-flag.
type: project
---

RESOLVED in MSG-K022 Sprint D Phase 2 (2026-04-07).

`Program.cs` now calls `app.UseForwardedHeaders()` with KnownProxies restricted to `IPAddress.Loopback` (127.0.0.1) and `KnownNetworks.Clear()` to remove default 10.x/8 trust. Middleware is placed before `UseAuthentication` and `UseRateLimiter` so the real client IP is visible to both subsystems.

**First seen:** 2026-04-05
**Resolved:** 2026-04-07 (MSG-K022)
**Remaining note:** `RequireHeaderSymmetry = false` — acceptable for single-header Nginx emit, tracked as WARNING W2 in MSG-K022 report.
