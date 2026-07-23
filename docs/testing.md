# Testing & Validation

## Validation path (before committing)

```powershell
# 1) Main app: clean build + compile-time static analysis (0 warnings / 0 errors)
dotnet build -c Debug

# 2) Core-layer pure-logic unit tests
dotnet test Tests\Core.Tests\Core.Tests.csproj -c Debug
```

## Compile-time analyzers

The main project enables .NET Roslyn analyzers
(`AnalysisMode=Recommended` + `EnforceCodeStyleInBuild`). Conventions carried
over from the .NET Framework original (public naming, constants, COM/Win32
interop) are intentionally "de-noised" in the root `.editorconfig` (downgraded
to `suggestion`/`none` with reasons) so the build stays at 0 warnings; all new
code is still held to warning level.

## Core unit tests

`Tests/Core.Tests` reuses `Core/` pure logic via **linked source** — fully
decoupled from WinUI, no WinExe assembly reference. Currently **51 cases, all
passing**:

| Target | Key paths covered |
|--------|-------------------|
| `RegistryEx` | Path split (`GetParentPath`/`GetKeyName`/`GetRootName`/`GetPathWithoutRoot`) and root mapping (`GetRootAndSubRegPath`: case-insensitive, root-only, unknown-root throws) |
| `GuidEx` | `IsGuid` / `TryParse` for 38- and 36-char GUIDs and various invalid inputs |
| `IniReader` | Section/key parsing, comment filtering, `=` trimming, case-insensitive, missing→empty, add/update/delete, duplicate-section ignored, empty input |
| `StringExtension` | `IsNullOrWhiteSpace` / `IsNullOrEmpty` / `IsEqual` (case-insensitive ordinal compare by default) |

## Notes

`dotnet build`'s final step writes the executable into `bin\`; if the app is
running (it self-elevates to an admin instance) it locks that file and causes
`MSB3027`. Close running instances before validation — compilation and analysis
are unaffected.
