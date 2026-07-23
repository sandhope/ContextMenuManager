# ContextMenuManager (WinUI 3)

> Windows 右键菜单管理器，基于 **WinUI 3 + .NET 10** 构建。

## 技术栈

| 组件 | 说明 |
|------|------|
| 框架 | .NET 10 + Windows App SDK（WinUI 3） |
| 目标框架 | `net10.0-windows10.0.26100.0` |
| 打包方式 | 非打包应用（`WindowsPackageType=None`） |
| 架构 | MVVM；注册表 / Shell 逻辑集中在 `Core/` |
| 图标 | Segoe MDL2 内置字形 |

## 功能

- **场景页** —— 文件、文件夹、目录、目录背景、桌面背景、磁盘分区、所有对象、此电脑、回收站、库等。
- **菜单编辑** —— 新建菜单（ShellNew）、发送到、打开方式、Win+X、增强菜单、详细编辑、GUID 锁。
- **程序字典** —— 查看内置 / 网络 / 用户字典文件。
- **程序设置、程序语言、关于**。

各功能页统一为卡片式列表：圆角卡片、紧凑 `ToggleSwitch` 控制可见性、透明图标按钮执行编辑/删除、注册表路径单行省略并悬停显示完整内容。

## 权限

启动时检测是否以管理员身份运行；非管理员时以 `runas` 重新拉起自身（触发 UAC）并在主窗口显示前退出当前实例。取消 UAC 则以当前权限继续运行（只读功能仍可用）。对 TrustedInstaller 拥有的注册表项，写入/删除前会编程夺取所有权。详见 [docs/permissions.md](docs/permissions.md)。

## 多语言

完全**代码式**，不依赖外部语言文件：译文以 `A("Section.Key", 中文, 英文)` 内嵌于 `Localization/AppStrings.cs`，`zh-CN` 为默认与回退语言，`en-US` 内联提供。详见 [docs/localization.md](docs/localization.md)。

## 前置依赖

- **.NET 10 SDK**：`global.json` 固定为 `10.0.302`（`rollForward: latestPatch`）。运行 `dotnet --version` 应输出 `10.0.3xx`。
- **Windows App SDK（WinUI 3）**：作为 NuGet 包随 `dotnet restore` 自动拉取，无需单独安装。
- **操作系统**：Windows 10 1809（`10.0.17763`）或更高；架构 `x86` / `x64` / `ARM64`。

详见 [docs/building.md](docs/building.md)。

## 构建与运行

```powershell
dotnet build -c Debug
dotnet run --launch-profile "ContextMenuManager (Unpackaged)"
```

本项目为非打包模式，**必须**选择 `Unpackaged` 启动 profile；直接 `dotnet run` 若落到 Package profile 会因缺少包标识而启动失败。首次运行会弹出 UAC 提权请求。

## 许可证

本项目遵循 [LICENSE](LICENSE) 中的许可条款。

## 支持项目

如果这个项目对你有帮助，欢迎请我喝杯咖啡 ☕

<table>
  <tr>
    <td>
      <img src="sponsor/weixin.jpg" width="200"/>
    </td>
    <td width="100" align="center" > 🙏 </td>
    <td>
      <img src="sponsor/alipay.jpg" width="200"/>
    </td>
  </tr>
</table>