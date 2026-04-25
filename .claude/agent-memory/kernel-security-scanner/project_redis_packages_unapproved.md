---
name: project_redis_packages_unapproved
description: StackExchange.Redis 2.8.* and Microsoft.Extensions.Caching.StackExchangeRedis 8.* added in MSG-K022 Sprint D Phase 2 — not on approved package list; no CVE; flag until CLAUDE.md updated.
type: project
---

StackExchange.Redis 2.8.* and Microsoft.Extensions.Caching.StackExchangeRedis 8.* added to SpaceOS.Infrastructure in MSG-K022 (Sprint D Phase 2) for distributed rate-limiting cache support.

**First seen:** 2026-04-07 (MSG-K022)
**Mitigation:** No CVE present. Flag as WARNING in every supply chain scan until packages are added to the approved list in CLAUDE.md. TLS support is configurable via Redis:UseTls config key. Password is read from Redis:Password config key — not hardcoded.
