---
name: joinery_pagingsize_unclamped
description: ListDoorOrders pageSize has no upper bound — authenticated DoS via large page request
type: project
---

ListDoorOrders endpoint accepts user-controlled `pageSize` query param with no maximum. A Manufacturer-authenticated user can request `pageSize=1000000`.

**First seen:** 2026-04-15, Joinery full security audit
**Mitigation:** Clamp pageSize in `DoorOrderEndpoints.cs` or `ListDoorOrdersQueryHandler`: `pageSize = Math.Clamp(pageSize, 1, 100)`. Re-flag as MEDIUM every scan until clamped.
