# Building & Running

## Prerequisites

- **.NET 10 SDK** — `global.json` pins `10.0.302` (`rollForward: latestPatch`,
  locks the `10.0.3xx` feature band, allows patch-level float). `dotnet --version`
  should print `10.0.3xx`. If no matching SDK is found, install from
  [.NET 10 download](https://dotnet.microsoft.com/download/dotnet/10.0).
- **`net10.0-windows` target framework** — ships with the .NET 10 SDK, no
  separate workload.
- **Windows App SDK (WinUI 3)** — pulled in as the `Microsoft.WindowsAppSDK`
  NuGet package on `dotnet restore`; no separate SDK/runtime install. On a
  restricted network, configure a nuget.org-reachable source beforehand.
- **OS** — Windows 10 1809 (`10.0.17763`) or later; architecture `x86` / `x64`
  / `ARM64`.

## Build & run

```powershell
# Build (auto-restores Windows App SDK etc.)
dotnet build -c Debug

# Run — non-packaged mode (WindowsPackageType=None) needs the Unpackaged profile
dotnet run --launch-profile "ContextMenuManager (Unpackaged)"
```

First run pops the UAC prompt; `dotnet run` from a normal terminal elevates fine.

## Launch profiles

`Properties/launchSettings.json` has two profiles:

- `ContextMenuManager (Package)` — `commandName=MsixPackage` (packaged/MSIX).
- `ContextMenuManager (Unpackaged)` — `commandName=Project` **(use this)**.

Plain `dotnet run` may fall back to the Package profile and fail from a missing
package identity. Always pick **Unpackaged**.

## Build conventions

- **Parallel compilation is intentionally OFF**: `BuildInParallel=false` and
  `XamlEnableMarkupCompilationInParallel=false` — avoids the WinUI XAML compiler
  intermittently losing `LocalAssembly` (WMC1509) and misreporting XAML errors.
  Do not re-enable parallelism.
- **Build = verification**: `EnableNETAnalyzers`, `AnalysisLevel=latest`,
  `AnalysisMode=Recommended`, `EnforceCodeStyleInBuild=true`. Goal is a clean
  `dotnet build` with **0 warnings / 0 errors**.
- `Nullable=enable`, `ImplicitUsings=enable` globally.
- **Close running instances before rebuilding** — the running (elevated) app
  locks `ContextMenuManager.exe` in `bin\`, causing `MSB3021`/`MSB3027` (a file
  lock, *not* a compile error).
