---
name: project_forwarded_headers_resolved
description: RESOLVED (MSG-K022 2026-04-07) — UseForwardedHeaders now configured in Program.cs with KnownProxies restricted to loopback only and KnownNetworks cleared. Do not re-flag the missing ForwardedHeaders finding.
type: project
---

The `project_forwarded_headers_missing.md` finding (X-Forwarded-For read without ForwardedHeadersMiddleware) was resolved in MSG-K022 Sprint D Phase 2.

Program.cs now configures:
- ForwardedHeaders = XForwardedFor | XForwardedProto
- KnownProxies = { IPAddress.Loopback } (127.0.0.1 only)
- KnownNetworks.Clear() (default 10.x/8 trust removed)
- RequireHeaderSymmetry = false (acceptable for single-header Nginx emit — WARNING kept)
- app.UseForwardedHeaders() placed before UseAuthentication and UseRateLimiter

**First seen resolution:** 2026-04-07
**Mitigation:** Re-flag only if KnownProxies is broadened or middleware is removed.
