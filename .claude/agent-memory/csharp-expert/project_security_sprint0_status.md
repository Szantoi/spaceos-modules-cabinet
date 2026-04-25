---
name: Security Sprint 0 Status
description: P0-3 Hash Chain, P0-1 RS256 JWT, P0-4 DB role separation — all complete, 357 tests passing (2026-04-03)
type: project
---

Security Sprint 0 completed 2026-04-03.

**P0-3 — Hash Chain:** AuditEvent gains PreviousHash (GENESIS default). IAuditEventRepository.GetLastHashAsync added. AuditEventDispatcher groups by tenant, acquires IAuditWriteLock, fetches previous hash, chains hashes within batch. Two implementations: InProcessAuditWriteLock (Development), PostgresAdvisoryAuditWriteLock (Production).

**P0-1 — RS256 JWT:** Replaced HS256 with RS256 throughout. IRsaPublicKeyProvider interface in Application.Common. PemFileRsaPublicKeyProvider (Development, falls back to DevRsaKeyManager auto-gen). AzureKeyVaultRsaPublicKeyProvider (Production, reads Jwt:RsaPublicKeyPem). All test factories updated: JwtTestHelper.TestRsa and JwtTokenHelper.TestRsa use ephemeral RSA key pair per test process. appsettings.json uses RsaPublicKeyPem instead of SigningKey. docker-compose uses JWT_RSA_PUBLIC_KEY_PEM.

**P0-4 — DB role separation:** scripts/db-init.sql creates spaceos_audit_writer role (SELECT+INSERT only on AuditEvents). Revokes UPDATE+DELETE from spaceos_audit_writer, PUBLIC, and spaceos. Mounted as PostgreSQL init script in docker-compose.yml.

**Why:** Security hardening — hash chain prevents tampering, RS256 eliminates shared symmetric secret risk, DB roles enforce append-only at infrastructure level.

**How to apply:** Any new test WebApplicationFactory in "Testing" environment must register both IAuditWriteLock stub AND update PostConfigure<JwtBearerOptions> to use RsaSecurityKey(JwtTestHelper.TestRsa) or RsaSecurityKey(JwtTokenHelper.TestRsa). DevRsaKeyManager is public so Program.cs can access it.
