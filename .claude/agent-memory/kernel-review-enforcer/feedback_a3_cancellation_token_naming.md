---
name: A3 CancellationToken naming — recurring across all command handlers
description: CODE agent consistently names the CancellationToken parameter `cancellationToken` instead of the required `ct` in Handle method signatures.
type: feedback
---

Rule A3 (`CancellationToken` parameter always named `ct`) is violated in every command handler the CODE agent produces. The pattern is consistent: the `Handle(TRequest request, CancellationToken cancellationToken)` signature is used even though CLAUDE.md and the Application CLAUDE.md both mandate `ct`.

**Why recurring:** The CODE agent appears to follow MediatR's IRequestHandler interface documentation which uses `cancellationToken` as the idiomatic name. The project convention overrides this.

**Standard fix:** Rename the parameter in the signature and all usages within the method body. Pure mechanical rename — no logic change required. Affects every `Handle` method in every command and query handler file touched by a task.

**Files to scan at start of every review:**
```bash
grep -rn "CancellationToken cancellationToken" SpaceOS.Kernel.Application/ --include="*.cs"
```
Any match in a `Handle` method signature is an A3 violation. Query handlers also affected — check those too.
