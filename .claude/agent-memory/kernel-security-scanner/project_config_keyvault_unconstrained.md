---
name: project_config_keyvault_unconstrained
description: WARNING (downgraded from CRITICAL 2026-04-05) — ConfigKeyVaultService in prod branch is self-guarding (throws if config absent). NodeAuthService uses DevRsaKeyManager ephemeral key. Comment fixed. Re-flag as WARNING until Key Vault-backed prod implementations exist.
type: project
---

`SpaceOS.Infrastructure/DependencyInjection.cs` production `else` branch.

## Status as of 2026-04-05

**Comment fix applied:** The misleading "NEVER registered in non-development" comment has been replaced with an accurate description: `ConfigKeyVaultService` IS registered in the prod branch but is self-guarding — it throws `InvalidOperationException` at runtime if `Crypto:SigningKey` or `Crypto:EncryptionKey` are absent in non-Development environments. No dev fallback executes.

**Severity downgraded from CRITICAL to WARNING** because:
1. `ConfigKeyVaultService.GetSigningKeyAsync()` and `GetEncryptionKeyAsync()` explicitly throw in non-dev if config keys are absent. The `!_isDevelopment` guard is confirmed in source (Crypto/ConfigKeyVaultService.cs L38, L55).
2. The comment was misleading, not the behaviour — the behaviour is correct.

**Remaining WARNING:** `NodeAuthService` still uses `DevRsaKeyManager` (ephemeral in-memory RSA key) for node JWT signing/validation. A Key Vault-backed signing implementation does not yet exist. This is acceptable pre-production, but must be replaced before any multi-node production deployment.

**First seen:** 2026-04-04, MSG-K023 scan
**Comment fixed:** 2026-04-05
**Severity:** WARNING (was CRITICAL)
**Mitigation:** Build and register a Key Vault-backed `INodeAuthService` implementation in the production branch. Re-flag as WARNING every scan until this exists.
