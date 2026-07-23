# AGENTS.md — 工程入口

面向协作者与编码代理的工程导航。README 面向用户功能，本文件面向"如何构建、运行、
改动与校验"。所有条目均对应仓库现有配置，无隐含前提。

## 技术栈速览

- 框架：.NET 10 + Windows App SDK（WinUI 3），目标框架 `net10.0-windows10.0.26100.0`，
  最低支持 `10.0.17763.0`。
- 打包：非打包应用（`WindowsPackageType=None`）。
- 架构：MVVM（`Models/` `ViewModels/` `Views/`），注册表 / Shell / Win32 互操作集中在 `Core/`。
- 关键依赖（见 `ContextMenuManager.csproj`）：
  - `Microsoft.WindowsAppSDK` 2.3.1
  - `Microsoft.Windows.SDK.BuildTools` 10.0.28000.2270
  - `Microsoft.Windows.SDK.BuildTools.WinApp` 0.5.0（为非打包 WinUI 提供 `dotnet run` 支持）

## 前置环境

- 已安装 .NET 10 SDK（含 `net10.0-windows` 工作负载）。
- Windows 10 1809（17763）或更高版本；本机架构受支持：`x86` / `x64` / `ARM64`。
- Windows App SDK 运行时随包引用还原，无需单独安装工作负载。

## 构建

```powershell
dotnet build -c Debug
```

构建注意事项：

- **并行编译已刻意关闭**。`csproj` 中设置了 `BuildInParallel=false` 与
  `XamlEnableMarkupCompilationInParallel=false`，用于规避 WinUI XAML 标记编译器在
  **并行编译下偶发丢失 `LocalAssembly`（WMC1509）** 而误报 XAML 错误的问题。改动构建
  配置时请勿重新开启并行，否则会出现间歇性、难复现的 XAML 编译失败。
- **构建即校验**。项目已接入编译期静态分析（`EnableNETAnalyzers`、`AnalysisLevel=latest`、
  `AnalysisMode=Recommended`、`EnforceCodeStyleInBuild=true`）。严重级别由根
  `.editorconfig` 统一治理，目标是 `dotnet build` **干净通过（0 警告）**。`Core/` 层保留了
  原 WinForms 版本的公共命名与互操作写法，相关"有意为之"的降噪规则已在 `.editorconfig`
  中配置，新增代码仍受 Recommended 级别把关。
- `Nullable=enable`、`ImplicitUsings=enable` 全局生效。
- **重建前先关闭已运行实例**。程序运行时会锁定输出目录的 `ContextMenuManager.exe`，
  否则重建在最后的拷贝步骤会报 `MSB3021 / MSB3027`（文件被占用）——此为文件锁，
  **并非编译错误**。先退出应用（或结束对应进程）再执行 `dotnet build`。

## 运行与 UAC 提权

```powershell
dotnet run
```

- 存在两个启动 profile（`Properties/launchSettings.json`）：`(Package)` 与 `(Unpackaged)`。
  本项目为非打包模式（`WindowsPackageType=None`），**应选 `(Unpackaged)`**（`commandName=Project`）。
- `app.manifest` 保持 `asInvoker`（**非** `requireAdministrator`），以避免从非管理员终端
  `dotnet run` 报错 740。
- 提权在启动第一步由代码完成（见 `App.OnLaunched` → `RelaunchAsAdmin`）：
  - 非管理员时以 `runas` 重新拉起自身（触发 UAC）并退出当前实例，使 UAC 在主窗口显示前弹出。
  - 用户取消 UAC（Win32 1223）时以当前权限继续运行，只读功能仍可用。
- 对 **TrustedInstaller** 拥有的注册表项，写入 / 删除前会编程夺取所有权
  （`SeTakeOwnership` / `SeRestore` → `SetOwner` → 授予 `FullControl`），实现见
  `Core/RegTrustedInstaller.cs` 与 `Core/RegistryEx.cs`。
- 启动诊断日志写入输出目录：`startup.log`（分步追加）、`crash.log`（异常详情）。

## 变更校验路径

改动后按以下顺序自查：

1. `dotnet build -c Debug` 必须 0 警告 0 错误（分析器 + 代码风格已并入构建）。
2. `dotnet run`（Unpackaged profile）实测启动，确认 UAC 正常弹出、主窗口出现。
3. 若启动异常或页面报错，查看输出目录下的 `startup.log` / `crash.log` 定位。

> 目前仓库尚无单元测试与 CI，回归依赖"完整构建 + 手动启动 + 日志核对"。新增 `Core/`
> 纯逻辑时可考虑补充单元测试项目。

## i18n（代码内嵌约定）

完全代码式多语言，**不依赖任何外部语言文件**（无 `.ini` / `.resw`，无运行时解析；参考
`WinUIClash` / `SingBoxWin` 的 `I18nService` 设计）：

- 所有译文以 `A("Section.Key", 中文, 英文)` 登记于 `Localization/AppStrings.cs`。
- `zh-CN` 为默认与回退语言，`en-US` 内联提供；取值缺失时回退键名或中文基底。
- XAML 取值两条路径：`{StaticResource S}` + `TranslateConverter`（属性绑定），
  及 `x:Bind loc:Loc.Tr('Section.Key')`（OneTime）。
- 切换语言自增 `AppStrings.Version`；`TranslateConverter` 据此刷新，页面由 `MainWindow`
  重新导航重建以实时生效。
- i18n 资源（`Resources["S"]` / `Resources["Translate"]`）在 `App.OnLaunched` 中、
  `MainWindow` 构造前注册。
- **新增语言**：扩展 `AppLang`、译文数组下标、`BuiltInLanguages` 列表并补齐译文即可。

## Core 层职责边界

`Core/` 是与 UI 无关的底层能力层，承载从原 WinForms 版本移植的注册表 / Shell / Win32 /
COM 互操作逻辑，保留原版公共命名与常量。典型职责：

- 注册表访问与所有权夺取：`RegistryEx.cs`、`RegTrustedInstaller.cs`、`RegistryAccessException.cs`。
- 管理员 / 系统信息：`AdminHelper.cs`、`WinOsVersion.cs`。
- Shell 与场景解析：`ShellScenes.cs`、`ShellLink.cs`、`ObjectPath.cs`、`GuidEx.cs`、`GuidInfo.cs`。
- 配置与 INI / 桌面 ini：`AppConfig.cs`、`IniReader.cs`、`IniWriter.cs`、`DesktopIni.cs`。
- 通用基础设施：`ObservableObject.cs`、`RelayCommand.cs`、`ResourceString.cs`、`StringExtension.cs`。

边界约定：

- `Core/` 不引用 `Views/`，尽量不依赖 WinUI UI 类型；界面逻辑放在 `ViewModels/` 与 `Views/`。
- 数据模型放 `Models/`（`IMenuItem` / `MenuItem` / `ShellExItem`）。
- 用户可见文案不硬编码，统一走 `Localization/AppStrings.cs`。
