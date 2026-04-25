---
name: newtonsoft-json-test-project-unapproved
description: RESOLVED — Newtonsoft.Json 13.0.1 was removed from SpaceOS.Kernel.Tests in E2/T4
type: project
---

`Newtonsoft.Json` Version 13.0.1 was referenced in `SpaceOS.Kernel.Tests/SpaceOS.Kernel.Tests.csproj`. It was not on the approved package list.

**First seen:** E2/T2 supply chain audit, 2026-03-24

**Resolved:** E2/T4, 2026-03-24 — package reference confirmed removed from `SpaceOS.Kernel.Tests.csproj`. No `.csproj` file in the solution contains `Newtonsoft.Json` as of T4 SECURITY phase.

**Mitigation:** COMPLETE. No further action required. Do not re-flag in future scans unless the package reappears.
