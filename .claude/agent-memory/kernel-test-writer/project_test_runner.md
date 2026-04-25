---
name: project_test_runner
description: xUnit v3 self-hosted runner — use 'dotnet exec <dll>' not 'dotnet test' to run tests in this project.
type: project
---

`dotnet test` fails with missing assembly errors (TestPlatform / Newtonsoft.Json version mismatch) in this environment. This is a pre-existing environment issue, not caused by test code.

**How to run tests:** Use `dotnet exec` directly on the compiled DLL:

```bash
dotnet exec SpaceOS.Kernel.Tests/bin/Debug/net8.0/SpaceOS.Kernel.Tests.dll
dotnet exec SpaceOS.Kernel.Api.Tests/bin/Debug/net8.0/SpaceOS.Kernel.Api.Tests.dll
dotnet exec SpaceOS.Kernel.IntegrationTests/bin/Debug/net8.0/SpaceOS.Kernel.IntegrationTests.dll
```

**Always build first:** `dotnet build` then `dotnet exec`.

**Filter by test method:** Append `-method "FullyQualifiedTestName"`.

**Why:** xUnit v3 uses an in-process self-hosted runner embedded in the test DLL itself, bypassing the VSTest adapter and its missing dependency.
