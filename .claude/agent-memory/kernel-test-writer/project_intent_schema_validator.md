---
name: project_intent_schema_validator
description: IntentDataSchemaValidator is internal static — tested directly via InternalsVisibleTo; per-TradeType required properties and error message patterns.
type: project
---

`IntentDataSchemaValidator` is `internal static` in `SpaceOS.Kernel.Application.Common`.
The `SpaceOS.Kernel.Application` project exposes it to `SpaceOS.Kernel.Tests` via `InternalsVisibleTo` (declared in the `.csproj` via `<AssemblyAttribute>`).

**Location:** `SpaceOS.Kernel.Application/Common/IntentDataSchemaValidator.cs`

**How to use:**
Call `IntentDataSchemaValidator.Validate(json, tradeType)` directly in unit tests — no mocking needed because it is a pure static function.

**Per-TradeType required properties (what to assert against):**
| TradeType | Required properties |
|-----------|-------------------|
| Joinery | `material` (string), `dimensions` (object) |
| Plumbing | `pipeDiameter` (number), `fluidType` (string) |
| Electrical | `voltage` (number), `circuitCount` (number) |
| Architecture | `floorPlan` (string) |
| Mep | `systems` (array) |
| null (generic) | must be object or array, max depth 10, max 65 536 bytes |

**Error message patterns:**
- Size exceeded: contains `"65536"`
- Invalid JSON: contains `"not valid JSON"`
- Wrong root type: contains `"object"`
- Missing property: contains the property name

**Why:** Knowing these patterns avoids re-reading the source every session when writing new schema coverage tests.
