---
name: spacelayer-externalsourceurl-ssrf
description: ExternalSourceUrl on SpaceLayer aggregate is a stored field returned in SpaceLayerDto — SSRF risk deferred to federation execution layer
type: project
---

`SpaceLayerDto.ExternalSourceUrl` is mapped directly from the `SpaceLayer` aggregate and returned in paged query responses. No outbound HTTP call is made in the current T2 scope (read-only projection). However, when the federation execution layer is implemented, any code that uses `ExternalSourceUrl` to make an outbound HTTP request must be reviewed for SSRF.

**First seen:** E2/T2 security scan, 2026-03-24

**Mitigation:** At the federation layer — validate/allowlist `ExternalSourceUrl` values before making outbound calls. Do not pass user-controlled URLs directly to `HttpClient`. Re-run `spaceos-no-ssrf` custom rule when that epic is scoped.
