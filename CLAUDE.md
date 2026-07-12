# CLAUDE.md — SpaceOS.Cabinet terminál

> A Cabinet terminál a SpaceOS asztalosipari parametrikus domain motort fejleszti.
> **Skill:** `/spaceos-terminal` — inbox → build → test → outbox DONE
> **Spec:** `docs/tasks/new/SpaceOS_Cabinet_0.1_CoreFoundation_Architecture_v4.md` (v4 FINAL, 105KB)

---

## SESSION STARTUP/SHUTDOWN RITUAL

**Minden session elején:**
```bash
# 0. Datahaven státusz regisztráció — jelezd hogy dolgozol
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "cabinet",
    "status": "working",
    "currentTask": "Session started - checking inbox"
  }'

# 1. Inbox ellenőrzés
ls /opt/spaceos/docs/mailbox/cabinet/inbox/
grep -l "status: UNREAD" /opt/spaceos/docs/mailbox/cabinet/inbox/*.md 2>/dev/null
```

**Session végén (DONE/BLOCKED outbox után):**
```bash
# Datahaven státusz regisztráció — jelezd hogy befejeztél
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{\"terminal\":\"cabinet\",\"status\":\"idle\"}'
```

**Datahaven Dashboard:** https://datahaven.joinerytech.hu (token: `dev-token-spaceos-dashboard-2026`)
- Dashboard (`/`) — Cabinet státusz (WORKING/IDLE), inbox/outbox metrikák
- Kanban (`/kanban`) — Cabinet swimlane a Delivery track-en
- Teljes API: `docs/WORKFLOW.md` — "Datahaven Dashboard" szakasz

---

## PROJEKT KONTEXTUS

**Repo:** `spaceos-modules-cabinet`
**Stack:** .NET 8 + .NET 10 multi-target, pure NuGet library (nincs runtime service)
**Scope:** Cabinet 0.1 — A1–A11 axiómák (Geometry, Skeleton, Machining, Construction, Semantics, Advisory)
**Target:** 7 NuGet csomag, 230+ teszt, v0.1.0-alpha.1

### NuGet csomagok

| Csomag | Framework | Tartalom |
|---|---|---|
| `SpaceOS.Cabinet.Geometry` | netstandard2.1 | Vector3, AffineTransform, PartFrame, AssemblyFrame |
| `SpaceOS.Cabinet.Abstractions` | netstandard2.1 | ITenantStandardProvider, ISnapshotMigrator, IGeometryProjector |
| `SpaceOS.Cabinet.Domain` | net8.0;net10.0 | Skeleton aggregate, BaseCuboid, Part, Connection |
| `SpaceOS.Cabinet.Machining` | net8.0;net10.0 | MachiningFeature, MachiningSubject, HardwareReference |
| `SpaceOS.Cabinet.Construction` | net8.0;net10.0 | ConstructionRuleEngine, 10 default rule |
| `SpaceOS.Cabinet.Semantics` | net8.0;net10.0 | SemanticInferenceService, cache |
| `SpaceOS.Cabinet` (meta) | net8.0;net10.0 | Mindent behúz |

### Kritikus axiómák

- **A1:** Affin mátrix mindenhol — AffineTransform VO
- **A3:** BaseCuboid mint gyökér — Skeleton aggregate root
- **A8:** Platform-független Core — nincs UI/CAD/DB dependency
- **A10:** Szelektív újraszámítás — DependencyGraph Kahn topo-sort
- **A11:** Warning, sosem blokkolás — DesignAdvisory

### Security (v3 review — SEC-CAB)

- **SEC-CAB-1 CRITICAL:** NaN/Infinity guard minden geometriai számításban
- **SEC-CAB-2 HIGH:** Cross-tenant Part isolation (internal ctor)
- **SEC-CAB-3 HIGH:** Dimension overflow (MaxWidthMm=50000)
- **SEC-CAB-4 HIGH:** ConstructionRule timeout + iteration cap
- **SEC-CAB-6 HIGH:** Post-deserialize validation (FromSnapshot)

---

## SOLUTION STRUKTÚRA

```
SpaceOS.Modules.Cabinet.sln
  src/
    SpaceOS.Cabinet.Geometry/
    SpaceOS.Cabinet.Abstractions/
    SpaceOS.Cabinet.Domain/
    SpaceOS.Cabinet.Machining/
    SpaceOS.Cabinet.Construction/
    SpaceOS.Cabinet.Semantics/
    SpaceOS.Cabinet/                  ← meta package
  tests/
    SpaceOS.Cabinet.Tests/            ← xUnit, 230+ teszt target
  global.json                         ← SDK pin
  Directory.Build.props               ← közös config
```

---

## KÖTELEZŐ PIPELINE

```
1. mailbox/inbox/ legfrissebb UNREAD üzenet olvasása
2. Build: dotnet build -c Release → 0 error, 0 warning
3. Test: dotnet test → összes pass (net8.0 ÉS net10.0!)
4. mailbox/outbox/ DONE vagy BLOCKED üzenet
```

**FONTOS:** Mindkét target framework-re tesztelni kell:
```bash
dotnet test -f net8.0
dotnet test -f net10.0
```

---

## KÓDOLÁSI SZABÁLYOK

```csharp
// 1. Nincs public setter — minden property private set vagy init
// 2. Factory method-ok Result<T> return type
// 3. SEC-CAB-1: NaN/Infinity guard
public static Result<Vector3> Create(double x, double y, double z)
{
    if (double.IsNaN(x) || double.IsInfinity(x)) return Result.Invalid("NaN/Infinity");
    // ...
}

// 4. A8: NINCS System.IO, System.Net, EF Core, MediatR dependency
// 5. A11: Advisory, never blocking — DesignAdvisory.Warn(), not throw
```

---

## MAILBOX

```
mailbox/ → /opt/spaceos/docs/mailbox/cabinet/
  inbox/   ← Root feladatok
  outbox/  ← DONE / BLOCKED üzenetek
```
