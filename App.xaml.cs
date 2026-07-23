using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using ContextMenuManager.Core;
using ContextMenuManager.Localization;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ContextMenuManager;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>主窗口句柄，供 FileOpenPicker 等非打包 WinUI3 组件初始化使用</summary>
    internal static IntPtr MainWindowHandle
    {
        get
        {
            var app = App.Current as App;
            if(app?._window == null) return IntPtr.Zero;
            return WindowNative.GetWindowHandle(app._window);
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    internal static void Trace(string step)
    {
        try
        {
            File.AppendAllText(
                Path.Combine(AppContext.BaseDirectory, "startup.log"),
                $"[{DateTime.Now:HH:mm:ss.fff}] {step}\n");
        }
        catch { /* 忽略 */ }
    }

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        Trace("App.ctor enter");

        // 全局异常捕获：写日志 + 弹窗，避免静默消失看不出原因（GUI 不显示时用于定位）。
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            ReportCrash("AppDomain.UnhandledException", e.ExceptionObject as Exception);
        // UI 线程未处理异常：记录（含出错页面/操作上下文）并降级为可恢复错误（Handled=true），
        // 避免单个页面/操作的异常直接把整个应用崩到桌面（这正是“很多页面无法使用”的根因之一）。
        UnhandledException += (_, e) =>
        {
            ReportHandled("Application.UnhandledException", e.Exception);
            e.Handled = true;
        };

        try
        {
            InitializeComponent();
            Trace("App.InitializeComponent done");

            // 加载语言：优先使用用户上次选择（AppConfig.Language），缺失时回退到内嵌 zh-CN。
            // 注意：Application.Resources 在 App 构造函数尚未就绪，
            // 必须等到 OnLaunched 中才能访问，因此资源注册在 OnLaunched 里做。
            AppStrings.Instance.LoadLanguage(AppConfig.Language);
            Trace("i18n loaded");
        }
        catch (Exception ex)
        {
            Trace($"App.ctor EXCEPTION: {ex}");
            ReportCrash("App.ctor", ex);
        }
    }

    /// <summary>
    /// 当前操作/页面上下文，用于未处理异常归因（如“页面 ShellListPage (SideBar.File)”）。
    /// 由 MainWindow 在导航时更新；默认标识启动阶段。
    /// </summary>
    internal static string CurrentContext { get; set; } = "启动阶段";

    /// <summary>
    /// 追加写崩溃日志：带时间戳、来源与出错上下文，保留历史（不覆盖）。
    /// 这是崩溃时唯一可依赖的证据，写失败也绝不再抛异常，避免二次崩溃。
    /// </summary>
    private static void AppendCrashLog(string source, string context, string detail)
    {
        Trace($"CRASH [{source}] @ {context}: {detail}");
        var entry =
            "========================================\n" +
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{source}] @ {context}\n" +
            $"{detail}\n\n";
        try
        {
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "crash.log"), entry);
        }
        catch { /* 忽略写日志失败 */ }
    }

    /// <summary>致命/近致命异常（启动阶段等）：记录日志并弹窗提示，便于定位。</summary>
    private static void ReportCrash(string source, Exception? ex)
    {
        var detail = ex?.ToString() ?? "(null)";
        AppendCrashLog(source, CurrentContext, detail);
        MessageBoxW(IntPtr.Zero,
            $"启动/运行异常 [{source}] @ {CurrentContext}\n\n{detail}",
            "ContextMenuManager 错误", 0x10);
    }

    /// <summary>
    /// UI 线程可恢复异常：记录日志（含出错页面/操作上下文）并降级，保留应用级安全网。
    /// 不弹模态框，避免单页异常反复打断用户；证据保留在 crash.log 供事后归因。
    /// </summary>
    private static void ReportHandled(string source, Exception? ex)
    {
        AppendCrashLog(source, CurrentContext, ex?.ToString() ?? "(null)");
    }

    /// <summary>
    /// 以管理员身份(runas)重新启动自身。返回 true 表示已成功拉起提权进程，
    /// 调用方应退出当前非管理员实例；返回 false 表示用户取消 UAC 或启动失败，
    /// 此时应继续以当前权限运行（部分只读功能仍可用）。
    /// </summary>
    private static bool RelaunchAsAdmin()
    {
        try
        {
            // 优先使用真实 exe 路径；`dotnet run` 下 MainModule 指向已生成的 exe，可正常提权。
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                ?? Environment.ProcessPath;
            if(string.IsNullOrEmpty(exePath)) return false;

            var psi = new System.Diagnostics.ProcessStartInfo(exePath)
            {
                Verb = "runas",
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory
            };
            System.Diagnostics.Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            // 常见于用户在 UAC 弹窗点击“否”(Win32Exception 1223)：不再重启，继续当前实例。
            Trace($"RelaunchAsAdmin failed/cancelled: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Trace("OnLaunched enter");

        // 启动第一步即确保管理员权限：非管理员时以 runas 重新拉起自身（触发 UAC），
        // 随后退出当前实例。这样 UAC 在主窗口显示之前弹出，且不依赖 manifest
        // requireAdministrator（后者会导致非管理员终端 `dotnet run` 报错 740）。
        if(!AdminHelper.IsRunningAsAdmin() && RelaunchAsAdmin())
        {
            Trace("relaunch as admin, exiting current instance");
            Exit();
            return;
        }

        try
        {
            // 注册全局 i18n 资源：必须在 Application.Resources 可用后（OnLaunched）进行，
            // 且要早于 MainWindow 构造，避免 XAML 绑定 {StaticResource S}/Translate 时找不到。
            Resources["S"] = AppStrings.Instance;
            Resources["Translate"] = new TranslateConverter();
            Trace("i18n resources registered");

            _window = new MainWindow();
            Trace("MainWindow created");
            _window.Activate();
            Trace("MainWindow.Activate called");
        }
        catch (Exception ex)
        {
            Trace($"OnLaunched EXCEPTION: {ex}");
            ReportCrash("OnLaunched", ex);
        }
    }
}
