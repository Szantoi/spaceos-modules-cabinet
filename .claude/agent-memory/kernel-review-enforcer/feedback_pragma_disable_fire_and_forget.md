---
name: Pragma suppression on fire-and-forget tasks
description: CODE agent uses `#pragma warning disable CS4014` to silence unawaited task warnings instead of using discard variable assignment.
type: feedback
---

`AuditEventDispatcher.FireAndForgetSink` introduced `#pragma warning disable CS4014` / `#pragma warning restore CS4014` to suppress the unawaited-task warning for a fire-and-forget sink call.

**Why recurring:** The CODE agent silences CS4014 with a pragma rather than using the idiomatic discard pattern.

**Standard fix:**
Replace:
```csharp
#pragma warning disable CS4014
_sink.WriteAsync(...).ContinueWith(...);
#pragma warning restore CS4014
```
With:
```csharp
_ = _sink.WriteAsync(...).ContinueWith(...);
```

The discard variable (`_ =`) explicitly signals intentional non-awaiting to both the compiler and readers, requires no pragma, and satisfies the G6 rule.

**First seen:** MSG-K020 Sprint D Phase 1 — `AuditEventDispatcher.cs`
