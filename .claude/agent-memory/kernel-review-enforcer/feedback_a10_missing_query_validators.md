---
name: A10 missing companion validators for new query types in Tools/Queries
description: CODE agent introduces new query handlers in non-standard feature folders (e.g., Tools/Queries) without companion validator files — A10 violation.
type: feedback
---

Rule A10 violation: every command and query must have a companion validator (Application CLAUDE.md: "no exceptions").

**Why recurring:** When CODE agent creates new query handlers in new feature folders (like `Tools/Queries/`) it omits validators, likely because the folder is new and the pattern isn't copied from existing feature folders.

**Standard fix:** For each new `*Query.cs`, create a companion `*QueryValidator.cs` in the same folder with `internal sealed class` and `AbstractValidator<TQuery>`. Paginated queries validate TenantId not-empty, Page >= 1, PageSize >= 1 and <= max (50 for tools endpoints).

**How to apply:** After every review, scan all new `*Query.cs` files and verify a matching `*QueryValidator.cs` exists in the same directory.
