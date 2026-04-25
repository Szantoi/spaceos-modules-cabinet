---
name: joinery_questpdf_wildcard
description: QuestPDF pinned with wildcard 2024.12.* — supply chain risk
type: project
---

QuestPDF in `SpaceOS.Modules.Joinery.Infrastructure.csproj` is pinned as `2024.12.*`. QuestPDF is not on the CLAUDE.md approved package list and processes user-supplied strings (ClientName, component names) into PDF output.

**First seen:** 2026-04-15, Joinery full security audit
**Mitigation:** Pin to exact version (e.g., 2024.12.0). Add to approved list or schedule review. Re-flag as LOW every scan until exact-pinned and approved.
