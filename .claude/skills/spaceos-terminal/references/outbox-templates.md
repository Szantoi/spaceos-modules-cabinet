# Outbox üzenet sablonok

## DONE — általános feladat befejezése

```markdown
---
id: MSG-<TERMINAL>-<NNN>-DONE
from: <terminál>
to: root
type: done
priority: <az inbox prioritása>
status: UNREAD
ref: MSG-<TERMINAL>-<NNN>
created: YYYY-MM-DD
---

# MSG-<TERMINAL>-<NNN> DONE — <Feladat neve>

## Összefoglaló

| Fájl | Változás | Ok |
|---|---|---|
| `path/to/file.cs` | <mit változtattál> | <miért> |

Commit: `<hash>` — branch: `develop`

## Tesztek

- `<build parancs>` → 0 error
- `<test parancs>` → **<N> teszt zöld** / 0 fail

Utolsó sor:
```
Tests  N passed (N)
Duration  Xs
```

## Security review

- Input validation: <ellenőrizve / nem érintett>
- Authorization / RBAC: <ellenőrizve / nem érintett>
- RLS (ha Kernel): <érintett táblák RLS policy-val rendelkeznek>
- Sensitive data: token/secret nem kerül logba ✅

## Kockázatok / kérdések

Nincsenek.
```

---

## DONE — Kernel specifikus (kódfix)

```markdown
## Összefoglaló

<1-2 mondat mit javított / implementált>

| Fájl | Változás | Ok |
|---|---|---|
| `SpaceOS.Kernel.Domain/.../Entity.cs` | Close() FSM guard javítva | DomainException helyett Result.Error |
| `SpaceOS.Kernel.Tests/.../HandlerTests.cs` | +2 teszt a hibaesetekre | coverage bővítés |

Commit: `<hash>` — branch: `develop`

## Tesztek

- `dotnet build` → 0 error, 0 warning
- `dotnet test` → **<N> teszt zöld** / 0 fail (+<X> új teszt)

## Security review

- Input validation: FluentValidation validator érintett ✅
- Authorization: RequireAuthorization("WritePolicy") érintetlen ✅
- RLS: app.current_tenant_id scope érintetlen ✅
- Sensitive data: nincs log-ban ✅

## Kockázatok / kérdések

<Ha van migration szükséges: jelezd!>
<Ha van breaking change: jelezd!>
Nincsenek.
```

---

## DONE — Orchestrator specifikus (route fix / új endpoint)

```markdown
## Összefoglaló

| Fájl | Változás | Ok |
|---|---|---|
| `src/routes/proof.route.ts` | URL: `/api/tasks/` → `/api/flow-epics/` | Kernel endpoint névegyezés |
| `src/routes/proof.route.test.ts` | mock URL frissítve | teszt konzisztencia |

Commit: `<hash>` — branch: `develop`

## Tesztek

- `npm run build` → 0 TS hiba
- `npm test` → **<N>/N teszt zöld**

## Security review

- Zod validáció: érintetlen ✅
- requireAuth middleware: érintetlen ✅
- Rate limiting: érintetlen ✅
- Header injection: proxy headers sanitizálva ✅

## Kockázatok / kérdések

Nincsenek.
```

---

## DONE — E2E specifikus (új tesztfájl)

```markdown
## Eredmény

**<N> pass / <F> fail / <S> skipped** (<T> összesen)

Új fájl: `src/chain/<NN>-<slug>.chain.test.ts` — <X> teszt

## Tesztek

```
Test Files  N passed (N)
     Tests  N passed (N)
  Duration  Xs
```

## Lefedettség

- `<NN>-<slug>`: <X>/<X> zöld

## Kockázatok / kérdések

<Ha valamit probe-and-skip-el: miért, mikor oldható fel>
Nincsenek.
```

---

## DONE — Infra specifikus (deploy)

```markdown
## Összefoglaló

| Service | Korábbi commit | Új commit | Port | Státusz |
|---|---|---|---|---|
| Kernel | <régi> | <új> | 5000 | ✅ online |

## Ellenőrzések

- `pm2 status` / `systemctl status`: online ✅
- Health endpoint: `<URL>` → 200 ✅
- Backup: `publish.bak-<timestamp>` létezik ✅

## Kockázatok / kérdések

Nincsenek.
```

---

## BLOCKED sablon

```markdown
---
id: MSG-<TERMINAL>-<NNN>-BLOCKED
from: <terminál>
to: root
type: blocked
priority: high
status: UNREAD
ref: MSG-<TERMINAL>-<NNN>
created: YYYY-MM-DD
---

# MSG-<TERMINAL>-<NNN> BLOCKED — <Feladat neve>

## Mi blokkol

<Konkrét technikai leírás — mi hiányzik, mi nem működik, hibaüzenet>

## Mit próbáltam

1. <1. próbálkozás + eredmény>
2. <2. próbálkozás + eredmény>

## Kérés a root-tól

<Döntés / információ / másik terminál bevonása>
```

---

## Gyors döntési fa — DONE vagy BLOCKED?

```
Feladat kész?
  ├─ build + test zöld → DONE
  ├─ build/test fail, de javítható → javítsd, aztán DONE
  └─ nem tudod befejezni önállóan
       ├─ hiányzó endpoint / service → BLOCKED
       ├─ nem egyértelmű követelmény → BLOCKED
       └─ infrastruktúra hozzáférés kell → BLOCKED
```
