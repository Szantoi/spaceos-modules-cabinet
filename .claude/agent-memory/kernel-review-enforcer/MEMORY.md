# Memory Index

This directory contains persistent memory files for the spaceos-review-enforcer agent.

## Files

| File | Type | Description |
|------|------|-------------|
| `feedback_internal_validators.md` | feedback | `internal sealed` validators are silently skipped by `AddValidatorsFromAssembly`; always pass `includeInternalTypes: true`. |
| `project_i5_domain_events_design.md` | project | Rule I5 `builder.Ignore(DomainEvents)` is inapplicable — `AggregateRoot` uses a private field, no property exists; I5 is PASS-BY-DESIGN across all 5 configs. |
| `feedback_a3_cancellation_token_naming.md` | feedback | CODE agent always uses `cancellationToken` instead of `ct` in `Handle` signatures — scan all command/query handlers on every review. |
| `feedback_d4_new_aggregates_missing_events.md` | feedback | CODE agent introduces new AggregateRoot subclasses without AddDomainEvent calls in Create() or mutation methods — D4 violation on every new aggregate. |
| `feedback_infra_config_visibility.md` | feedback | CODE agent introduces IEntityTypeConfiguration as `public class` instead of `internal sealed class`; DbContext subclasses not sealed — check visibility on every new config and context. |
| `feedback_pragma_disable_fire_and_forget.md` | feedback | CODE agent uses `#pragma warning disable CS4014` for unawaited fire-and-forget tasks — always use `_ = task` discard pattern instead. |
| `project_i6_repo_visibility_integration_test_conflict.md` | project | `AuditEventRepository` cannot be made `internal` without breaking integration tests that directly instantiate it. Requires `InternalsVisibleTo` or DI refactor. |
| `feedback_xunit1051_cancellation.md` | feedback | New API test files call `GetAsync`/`PostAsJsonAsync` without `TestContext.Current.CancellationToken` — always pass token to all HttpClient calls in Api.Tests. |
| `feedback_p2_results_unauthorized.md` | feedback | CODE agent uses `Results.Unauthorized()` in endpoint early-return paths instead of `Results.Problem(statusCode: 401)` — scan all new endpoints for `Results.Unauthorized/NotFound/BadRequest`. |
| `feedback_a10_missing_query_validators.md` | feedback | New query handlers in new feature folders (Tools/Queries) omit companion validators — verify `*QueryValidator.cs` exists alongside every `*Query.cs`. |
