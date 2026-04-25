---
name: project_node_auth_service_unconstrained
description: NodeAuthService (uses DevRsaKeyManager ephemeral key) registered unconditionally — no production Key Vault binding for node JWT signing
type: project
---

`SpaceOS.Infrastructure/DependencyInjection.cs` production `else` branch line 137 (confirmed 2026-04-05):
`services.AddSingleton<INodeAuthService, NodeAuthService>()` — `NodeAuthService` uses `DevRsaKeyManager.Instance`.

`DevRsaKeyManager` loads/creates `keys/dev-private-key.pem` at CWD. `keys/` is NOT listed in `.dockerignore` (only `**/*.pem` files are excluded by extension). `.gitignore` covers `*.pem`. Private key file exposure risk in Docker layer is mitigated by `**/*.pem` in `.dockerignore`.

This finding is now subsumed by `project_config_keyvault_unconstrained.md` which covers both `ConfigKeyVaultService` and `NodeAuthService` being registered in the production else-branch.

**First seen:** 2026-04-04, MSG-K024 scan
**Confirmed still present:** 2026-04-05, full codebase scan
**Severity:** CRITICAL (part of CF-1)
**Mitigation:** Same as `project_config_keyvault_unconstrained.md` — replace production DI registrations with Key Vault-backed implementations. Re-flag every scan.
