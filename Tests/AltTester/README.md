# Othello E2E (AltTester)

End-to-end tests that drive the actual Unity game (Editor Play or built Player)
over AltTester's WebSocket protocol. Tests are pure C# / NUnit, run with
`dotnet test`, and live outside `Assets/` so they never get built into the game.

## One-time setup

1. **Install AltTester Unity SDK.** Already wired into `Packages/manifest.json`
   via OpenUPM. Open the project in Unity once to let the package resolve.
2. **Instrument builds.** In Unity: `AltTester > AltTester Editor`. Toggle
   "AltTester" ON for your build target. This injects the in-game server.
3. **Get the AltTester Server.** Download from
   <https://alttester.com/alttester-server/> and launch it. Default port: 13000.
4. **.NET SDK 8+** for running tests.

## Run

```sh
# In Unity Editor: hit Play (instrumented) — OR — run an instrumented Player build.
# AltTester Server desktop app must be running.

cd Tests/AltTester
dotnet test
```

Per-test screenshots are written to
`Tests/AltTester/bin/.../artifacts/<TestName>.png` — handy for AI review and CI
artifacts.

## Adding a test

`AltDriver` exposes the running Unity scene:

```csharp
_driver.FindObject(By.NAME, "Btn_vs_ai").Tap();
var home = _driver.WaitForObject(By.NAME, "Btn_title_btn", timeout: 5);
Assert.That(home.enabled, Is.True);
```

For internal-state assertions (e.g. checking `OthelloGameManager._currentPlayer`
without going through events), use `CallStaticMethod` / `GetComponentProperty`.

## Why C# (not Python)

- Same project language as the game itself.
- AltTester's data surface is identical across SDKs — language choice doesn't
  affect what you can inspect.
- Keeps the toolchain to one runtime (.NET).
