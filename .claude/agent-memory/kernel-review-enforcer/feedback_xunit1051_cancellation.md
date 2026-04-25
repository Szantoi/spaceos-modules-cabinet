---
name: xUnit1051 CancellationToken in API tests
description: New API test files use HttpClient.GetAsync/PostAsJsonAsync without TestContext.Current.CancellationToken — triggers xUnit1051.
type: feedback
---

CODE agent introduces new `SpaceOS.Kernel.Api.Tests` test files calling `await _client.GetAsync(url)` and `await _client.PostAsJsonAsync(url, body)` without passing a `CancellationToken`. xUnit v3 analyzer xUnit1051 requires `TestContext.Current.CancellationToken` in all method calls that accept a token.

**Why recurring:** HttpClient extension methods have `CancellationToken`-accepting overloads. The CODE agent uses the parameterless overloads.

**Standard fix:**
- `await _client.GetAsync(url)` → `await _client.GetAsync(url, TestContext.Current.CancellationToken)`
- `await _client.PostAsJsonAsync(url, body)` → `await _client.PostAsJsonAsync(url, body, TestContext.Current.CancellationToken)`
- `await response.Content.ReadAsStringAsync()` → `await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)`

Scan all new `*Tests.cs` files in `SpaceOS.Kernel.Api.Tests/` with: `grep -rn "await.*GetAsync\|await.*PostAsJsonAsync\|await.*ReadAsStringAsync" SpaceOS.Kernel.Api.Tests/ | grep -v "CancellationToken"`
