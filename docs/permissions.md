# Permissions & UAC Elevation

This app edits system registry keys (including TrustedInstaller-owned ones),
so it needs administrator rights. Elevation is handled in code, not via the
manifest.

## UAC elevation flow

On launch (`App.OnLaunched` → `RelaunchAsAdmin`):

- If **not** running as admin, it relaunches itself with `runas` (triggers UAC)
  and exits the current instance — so the UAC prompt appears *before* the main
  window.
- If the user **cancels** UAC (Win32 error `1223`), it continues with current
  privileges — read-only features still work.

`app.manifest` stays `asInvoker` (not `requireAdministrator`) to avoid error
`740` when running `dotnet run` from a non-admin terminal.

## TrustedInstaller-owned keys

Some registry keys (e.g. parts of `HKEY_CLASSES_ROOT\...\shell`) are owned by
**TrustedInstaller**. Before writing/deleting, the app programmatically takes
ownership:

1. Enable `SeTakeOwnership` / `SeRestore` privileges.
2. `SetOwner` to the current admin principal.
3. Grant `FullControl`.

Implementation: `Core/RegTrustedInstaller.cs` and `Core/RegistryEx.cs`.
