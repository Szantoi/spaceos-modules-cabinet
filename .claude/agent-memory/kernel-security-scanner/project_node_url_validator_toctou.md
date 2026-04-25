---
name: project_node_url_validator_toctou
description: PARTIALLY RESOLVED (2026-04-05) — IPv4-mapped IPv6 bypass fixed. DNS TOCTOU gap remains — documented in XML, caller responsibility. Re-flag DNS gap as WARNING (not CRITICAL) until federation layer adds runtime DNS check.
type: project
---

`SpaceOS.Infrastructure/Validation/NodeUrlValidator.cs`

## IPv4-mapped IPv6 bypass — RESOLVED 2026-04-05

`ip.IsIPv4MappedToIPv6` check added in the IPv6 branch. `MapToIPv4()` called and result passed to the new `internal static IsPrivateIPv4(byte[])` helper. The `::ffff:0:0/96` range is now blocked. Addresses like `::ffff:192.168.1.1` are correctly rejected.

## DNS TOCTOU gap — OPEN (WARNING, not CRITICAL)

For DNS hostnames (non-IP-literal hosts), the validator only rejects `localhost`, `.local`, and `.internal` suffixes. It does not resolve DNS. A DNS rebinding attack remains theoretically possible.

**Mitigation documented:** XML `<remarks>` added to the class explaining the limitation and requiring callers (federation execution layer) to perform DNS resolution + IP check before every outbound connection.

**Status:** No federation outbound calls exist today. The risk is latent. Re-flag as WARNING when the federation execution layer is built. Severity downgraded from CRITICAL to WARNING because:
1. IPv4-mapped bypass (the concrete exploit path) is now fixed.
2. No code currently makes outbound HTTP calls using `ExternalSourceUrl`.

**First seen:** 2026-04-04, MSG-K025 scan
**IPv4-mapped bypass fixed:** 2026-04-05
**DNS TOCTOU:** Accepted latent risk — carrier: `project_spacelayer_ssrf_risk.md`
