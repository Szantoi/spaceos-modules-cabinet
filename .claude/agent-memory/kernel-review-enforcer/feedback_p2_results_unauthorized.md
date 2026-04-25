---
name: P2 Results.Unauthorized() in new endpoints
description: CODE agent uses Results.Unauthorized() in endpoint early-return paths instead of Results.Problem(statusCode: 401) — violates P2 on every new endpoint group with tenant claim extraction.
type: feedback
---

Rule P2 violation: `Results.Unauthorized()` produces a plain 401 with no body. ProblemDetails is required for all error responses.

**Why recurring:** When CODE agent adds endpoints that extract tenant from JWT claims and return early (e.g., `if (tenantId == Guid.Empty) return Results.Unauthorized()`), it uses the convenience shorthand instead of the required ProblemDetails form.

**Standard fix:**
```csharp
// Before:
return Results.Unauthorized();

// After:
return Results.Problem(
    title:      "Unauthorized",
    detail:     "A valid JWT Bearer token with a tenant claim is required.",
    statusCode: 401,
    type:       "https://tools.ietf.org/html/rfc7235#section-3.1");
```

**How to apply:** Grep every new endpoint file for `Results.Unauthorized()` — it should never appear. The same applies to `Results.NotFound()` and `Results.BadRequest()` which also lack ProblemDetails.
