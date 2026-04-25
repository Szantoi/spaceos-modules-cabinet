---
name: MSG-K022 DB Migrations Sprint C Phase 3
description: MSG-K022 Sprint C Phase 3 DB-01..DB-10 CLOSED_DONE — 539 tests passing (2026-04-04)
type: project
---

MSG-K022 Sprint C Phase 3 (DB-01..DB-10) is CLOSED_DONE as of 2026-04-04.
539 tests passing (401 unit + 46 integration + 92 API).

**Why:** Database schema foundation for FlowManagement module and federation sync tables.

**How to apply:** ModulesDbContext and its 5 entity configurations are in SpaceOS.Modules.FlowManagement.
ITenantConnectionResolver is in SpaceOS.Kernel.Domain.Auth. SharedTenantConnectionResolver is Level 1 (shared DB).
PostgresSchemaInitializer.ApplyAsync is public and is called from Program.cs in non-dev environments.
ModulesDbContext uses ProviderName string check instead of IsNpgsql() to avoid Npgsql package dependency in the module.
