---
name: project_cicd_action_pinning
description: GitHub Actions in ci.yml use mutable version tags (@v4, @v1) — not pinned to commit SHA. appleboy/ssh-action is third-party with SSH key access.
type: project
---

GitHub Actions in `.github/workflows/ci.yml` reference all actions by mutable version tags (`actions/checkout@v4`, `actions/setup-dotnet@v4`, `appleboy/ssh-action@v1`).

**First seen:** MSG-K020 Sprint D Phase 1 scan (2026-04-06)

**Mitigation:** Pin each action to its full commit SHA digest, especially `appleboy/ssh-action` which receives the `VPS_DEPLOY_KEY` secret. Re-flag as WARNING in every CI/CD scan until pinned.
