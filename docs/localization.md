# Localization (i18n)

Fully **code-based** — no external language files (no `.ini` / `.resw`, no
runtime parsing). Design inspired by `WinUIClash` / `SingBoxWin`'s
`I18nService`.

## How it works

- All strings are registered as strongly-typed code:
  `A("Section.Key", 中文, 英文)` in `Localization/AppStrings.cs`.
- `zh-CN` is the default and fallback language; `en-US` is inlined.
  Missing keys fall back to the key name or the Chinese base.
- XAML reads values two ways:
  - `{StaticResource S}` + `TranslateConverter` (property bindings).
  - `x:Bind loc:Loc.Tr('Section.Key')` (OneTime).
- Switching language increments `AppStrings.Version`; `TranslateConverter`
  refreshes and `MainWindow` re-navigates to rebuild pages live.

## i18n resources

Resources (`Resources["S"]` / `Resources["Translate"]`) are registered in
`App.OnLaunched`, **before** `MainWindow` is constructed.

## Adding a new language

Extend `AppLang`, the translation array index, and the `BuiltInLanguages` list,
then fill in the translations.
