---
name: project_cicd_double_build
description: ci.yml deploy job discards the CI-built artifact and re-builds from git on the VPS — the deployed artifact is not the same one that passed tests.
type: project
---

The `deploy` job in `.github/workflows/ci.yml` runs `dotnet publish` on the CI runner but discards the output. The VPS deployment script does `git pull` then `dotnet publish` again on the server. This means:

1. The artifact running in production was never validated by the CI test suite.
2. Code pushed to `main` between the CI `test` job completing and the deploy step running could be deployed without test coverage.
3. Production VPS requires the full .NET SDK (not just the runtime).

**First seen:** MSG-K020 Sprint D Phase 1 scan (2026-04-06)

**Mitigation:** Either transfer the runner-built artifact (scp/rsync + systemctl restart) or remove the runner-side `dotnet publish` step and document server-side build as intentional. Re-flag as WARNING until resolved.
