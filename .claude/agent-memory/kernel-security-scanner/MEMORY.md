# Memory Index

This directory contains persistent memory files for the SpaceOS Security Scanner agent.

## Files

| File | Type | Description |
|------|------|-------------|
| `project_spacelayer_ssrf_risk.md` | project | ExternalSourceUrl on SpaceLayer is a stored field returned read-only today; SSRF risk must be evaluated when the federation execution layer makes outbound HTTP calls using it. |
| `project_newtonsoft_json_tests.md` | project | RESOLVED (E2/T4) — Newtonsoft.Json 13.0.1 was removed from SpaceOS.Kernel.Tests. No longer present in any .csproj. Do not re-flag. |
| `project_microsoft_data_sqlite_tests.md` | project | Microsoft.Data.Sqlite 8.0.11 in Api.Tests and IntegrationTests is not on the approved package list — test-only, no known CVE, flag in every supply chain scan until removed or approved. |
| `project_npgsql_supply_chain.md` | project | Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11 added in E6/T1 — not on original approved package list; no CVE; flag until REVIEW_CHECKLIST.md is updated. |
| `feedback_ef_model_probe_dummy_connstring.md` | feedback | UseNpgsql with dummy credentials in tests that only inspect IModel metadata is accepted risk — no live connection opened, credentials are inert. |
| `project_ct_naming_drift.md` | project | Query handlers and event handlers in Application layer still use 'cancellationToken' instead of 'ct' — naming convention deviation only, no security risk, cleanup deferred (found E6/T4). |
| `project_appsettings_baked_into_image.md` | project | RESOLVED (E7/T1 final re-scan 2026-03-27) — `**/*Testing*` added to `.dockerignore`; appsettings.Testing.json is now excluded from build context. Do not re-flag unless pattern is removed or a new credential file escapes the exclusion. |
| `project_appsettings_development_jwt_key.md` | project | RESOLVED (2026-04-03) — appsettings.Development.json JWT key finding closed; file now contains only SQLite DSN and is excluded from git tracking. Do not re-flag. |
| `project_db_init_sql_hardcoded_password.md` | project | WARNING (downgraded from ERROR 2026-04-05) — scripts/db-init.sql creates spaceos_audit_writer role with no PASSWORD clause. Runtime injection documented in comments. Re-flag as WARNING every scan. |
| `project_inode_url_validator_missing.md` | project | SUPERSEDED by project_node_url_validator_toctou.md — concrete implementation now exists (K025); TOCTOU/DNS-rebinding gap is the active finding. |
| `project_inprocess_sync_lock_production.md` | project | RESOLVED (2026-04-05) — InProcessSyncSignalWriteLock replaced with PostgresAdvisorySyncSignalWriteLock in production DI branch. Do not re-flag. |
| `project_config_keyvault_unconstrained.md` | project | WARNING (downgraded from CRITICAL 2026-04-05) — ConfigKeyVaultService in prod branch is self-guarding (throws if config absent). Comment fixed. NodeAuthService still uses DevRsaKeyManager ephemeral key — Key Vault-backed prod implementation needed. Re-flag as WARNING. |
| `project_node_auth_service_unconstrained.md` | project | WARNING (subsumed by project_config_keyvault_unconstrained, downgraded from CRITICAL 2026-04-05) — NodeAuthService uses DevRsaKeyManager ephemeral key in prod. Key Vault-backed implementation needed. |
| `project_node_url_validator_toctou.md` | project | PARTIALLY RESOLVED (2026-04-05) — IPv4-mapped IPv6 bypass (::ffff:0:0/96) fixed. DNS TOCTOU gap documented in XML, caller responsibility. Re-flag DNS gap as WARNING when federation execution layer is built. |
| `project_forwarded_headers_missing.md` | project | RESOLVED (MSG-K022 2026-04-07) — UseForwardedHeaders now in Program.cs with KnownProxies=loopback and KnownNetworks cleared. Do not re-flag. |
| `project_cicd_action_pinning.md` | project | WARNING (found MSG-K020 2026-04-06) — GitHub Actions in ci.yml use mutable version tags; appleboy/ssh-action@v1 is third-party with SSH key access. Pin to commit SHA. |
| `project_cicd_double_build.md` | project | WARNING (found MSG-K020 2026-04-06) — deploy job discards CI-built artifact; VPS re-builds from git, so deployed artifact was never validated by CI tests. |
| `project_refresh_token_role_loss.md` | project | WARNING (found MSG-K021 2026-04-06) — RefreshTokenCommandHandler issues rotated access token with hardcoded "User" role and Guid.Empty tenantId; original role/tenant not preserved. |
| `project_refresh_token_service_duplicate.md` | project | WARNING (found MSG-K021 2026-04-06) — RefreshTokenService in Infrastructure duplicates RefreshTokenHasher in Application; Infrastructure copy appears unused, divergence risk. |
| `project_jwt_packages_unapproved.md` | project | WARNING (found MSG-K021 2026-04-06) — Microsoft.IdentityModel.Tokens 7.6.0, System.IdentityModel.Tokens.Jwt 7.6.0, JwtBearer 8.0.11 not on approved list; no CVE; flag until CLAUDE.md updated. |
| `project_redis_packages_unapproved.md` | project | WARNING (found MSG-K022 2026-04-07) — StackExchange.Redis 2.8.* and Microsoft.Extensions.Caching.StackExchangeRedis 8.* not on approved list; no CVE; flag until CLAUDE.md updated. |
| `project_forwarded_headers_resolved.md` | project | RESOLVED (MSG-K022 2026-04-07) — UseForwardedHeaders now configured with KnownProxies=loopback-only and KnownNetworks cleared. Do not re-flag the missing ForwardedHeaders finding. |
| `project_allowlist_rls_missing_coalesce.md` | project | ERROR (found MSG-K030 2026-04-09) — TenantHandshakeAllowlist RLS policy (Migration 0026) missing COALESCE sentinel; throws on empty current_tenant_id. Needs Migration 0028 to fix. |
| `project_allowlist_ignorequeryfilters_archived.md` | project | WARNING (found MSG-K030 2026-04-09) — IgnoreQueryFilters() on Tenants join in TenantHandshakeAllowlistRepository leaks archived tenant names into allowed_hosts JWT claim. |
| `project_jwt_authority_silent_fallback.md` | project | WARNING (found MSG-K031 2026-04-09) — ConfigureJwtBearerOptions silently falls back to hardcoded issuer string when JWT_AUTHORITY and Jwt:Issuer are both absent. Needs fail-fast guard in non-Development. |
| `project_joinery_pagingsize_unclamped.md` | project | MEDIUM (found 2026-04-15) — ListDoorOrders pageSize has no upper bound; authenticated DoS via large page request. Clamp to 100. |
| `project_joinery_questpdf_wildcard.md` | project | LOW (found 2026-04-15) — QuestPDF pinned with wildcard 2024.12.*; not on approved list; processes user strings into PDF. Pin to exact version. |
| `project_joinery_jwt_audience_disabled.md` | project | MEDIUM (found 2026-04-15) — ValidateAudience=false in Joinery Program.cs; cross-service token replay possible. Enable once Kernel JWT includes aud claim. |
