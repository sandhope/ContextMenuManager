#nullable disable
using System;
using System.Runtime.InteropServices;
using ContextMenuManager;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace ContextMenuManager.Core;

/// <summary>
/// 纯 Win32 实现的系统托盘图标（通知区域），零第三方依赖。
/// 实现方式参考已验证的 SingBoxWin / WinSing 参考工程：
/// 自建一个 <b>message-only 窗口</b>（HWND_MESSAGE），把它的 WndProc 通过
/// RegisterClassExW 注册进去，由该窗口接收 Shell 的托盘回调消息。
/// <b>不要子类化 WinUI 3 主窗口的 WndProc</b>——WinUI 3 的消息循环对独立
/// message-only 窗口（自带函数指针 WndProc）能稳定派发消息，对子类化主窗口则不可靠。
/// 行为与需求一致：
///   <list>
///     <item>左键单击或双击托盘图标 → 显示并激活主窗口；</item>
///     <item>右键托盘图标 → 弹出菜单（显示主窗口 / 退出）；</item>
///     <item>点击主窗口关闭按钮(X) → 直接退出，关闭时清理托盘图标。</item>
///   </list>
/// <b>注意：这里刻意不调用 NIM_SETVERSION(NOTIFYICON_VERSION_4)</b>，沿用经典回调契约——
/// 这样 WM_LBUTTONUP(单击) / WM_LBUTTONDBLCLK(双击) / WM_RBUTTONUP(右键) 才会按原始鼠标
/// 消息稳定送达（参考已验证的 SingBoxWin 参考工程，它同样未启用版本 4）。
/// </summary>
internal sealed class TrayIconManager : IDisposable
{
    private const uint WM_USER = 0x0400;
    private const uint TrayCallbackMessage = WM_USER + 1;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_LBUTTONDBLCLK = 0x0203;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_NULL = 0x0000;

    // shellapi.h 真实取值（之前误写成 0x0001/0x0003，导致从未真正 ADD）
    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_DELETE = 0x00000002;

    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;

    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x0010;
    private const int IDI_APPLICATION = 32512;

    private const uint MF_STRING = 0x0000;
    private const uint MF_SEPARATOR = 0x0800;
    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint TPM_RETURNCMD = 0x0100;

    private const uint CmdShow = 1001;
    private const uint CmdExit = 1002;

    private const string ClassName = "ContextMenuManagerTrayMsgClass";

    private readonly Window _owner;        // 主窗口，用于 Show/Activate
    private readonly WndProc _wndProc;      // 故意作为 GC 根保留，避免被回收
    private IntPtr _msgHwnd;               // message-only 窗口，接收回调
    private IntPtr _hIcon;
    private NOTIFYICONDATA _data;
    private bool _added;

    public TrayIconManager(Window owner)
    {
        _owner = owner;
        _wndProc = WndProcThunk;
        _msgHwnd = CreateMessageWindow();
    }

    /// <summary>加载图标并向 Shell 注册托盘。建议在窗口 Activate 之后调用。</summary>
    public void Initialize(string tooltip)
    {
        try
        {
            string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            _hIcon = LoadIconFromFile(iconPath);

            _data = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _msgHwnd,                 // 回调发往 message-only 窗口，而非 WinUI 主窗口
                uID = 1,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = TrayCallbackMessage,
                hIcon = _hIcon,
                szTip = tooltip,
            };

            bool ok = Shell_NotifyIconW(NIM_ADD, ref _data);
            App.Trace($"TrayIcon NIM_ADD => {ok}");
            _added = ok;
            if (!ok)
            {
                App.Trace($"TrayIcon: Shell_NotifyIcon NIM_ADD 失败（LastError={Marshal.GetLastWin32Error()}）");
            }
        }
        catch (Exception ex)
        {
            App.Trace($"TrayIcon init failed: {ex}");
        }
    }

    private IntPtr CreateMessageWindow()
    {
        var hInstance = GetModuleHandleW(null);

        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = hInstance,
            lpszClassName = ClassName,
        };
        // 忽略“类已注册”（同进程内重复构造）
        RegisterClassExW(ref wc);

        IntPtr hwnd = CreateWindowExW(
            0, ClassName, null, 0,
            0, 0, 0, 0,
            HWND_MESSAGE, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (hwnd == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create tray message-only window.");
        return hwnd;
    }

    private IntPtr WndProcThunk(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == TrayCallbackMessage)
        {
            // 经典回调（不启用 NOTIFYICON_VERSION_4）：lParam = 鼠标消息
            uint mouse = (uint)lParam.ToInt32();
            if (mouse == WM_LBUTTONUP || mouse == WM_LBUTTONDBLCLK) ShowMainWindow();
            else if (mouse == WM_RBUTTONUP) ShowContextMenu();
            return IntPtr.Zero;
        }
        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private void ShowMainWindow()
    {
        _owner.DispatcherQueue.TryEnqueue(() =>
        {
            _owner.AppWindow.Show();
            _owner.Activate();
        });
    }

    private void ShowContextMenu()
    {
        IntPtr hMenu = CreatePopupMenu();
        if (hMenu == IntPtr.Zero) return;

        AppendMenuW(hMenu, MF_STRING, CmdShow, "显示主窗口");
        AppendMenuW(hMenu, MF_SEPARATOR, 0, string.Empty);
        AppendMenuW(hMenu, MF_STRING, CmdExit, "退出");

        GetCursorPos(out POINT pt);
        SetForegroundWindow(_msgHwnd);   // 使菜单在外部点击时正确消失
        uint cmd = TrackPopupMenuEx(hMenu, TPM_RIGHTBUTTON | TPM_RETURNCMD, pt.X, pt.Y, _msgHwnd, IntPtr.Zero);
        DestroyMenu(hMenu);
        PostMessageW(_msgHwnd, WM_NULL, IntPtr.Zero, IntPtr.Zero);

        if (cmd == CmdShow) ShowMainWindow();
        else if (cmd == CmdExit) ExitApplication();
    }

    private void ExitApplication()
    {
        Dispose();
        Environment.Exit(0);
    }

    public void Dispose()
    {
        if (_added)
        {
            Shell_NotifyIconW(NIM_DELETE, ref _data);
            _added = false;
        }
        if (_msgHwnd != IntPtr.Zero)
        {
            DestroyWindow(_msgHwnd);
            _msgHwnd = IntPtr.Zero;
        }
        if (_hIcon != IntPtr.Zero)
        {
            DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }
    }

    /// <summary>从文件加载图标；失败回退到系统默认应用图标。</summary>
    private static IntPtr LoadIconFromFile(string path)
    {
        if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
        {
            IntPtr h = LoadImageW(IntPtr.Zero, path, IMAGE_ICON, 0, 0, LR_LOADFROMFILE);
            if (h != IntPtr.Zero) return h;
        }
        return LoadIconW(IntPtr.Zero, (IntPtr)IDI_APPLICATION);
    }

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // ---- structs ----
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEXW
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
        public IntPtr hIconSm;
    }

    private static readonly IntPtr HWND_MESSAGE = new(-3);

    // ---- P/Invoke ----
    [DllImport("shell32", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIconW(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassExW(ref WNDCLASSEXW lpwcx);

    [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowExW(
        uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string lpModuleName);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenuW(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    private static extern uint TrackPopupMenuEx(IntPtr hMenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    [DllImport("user32")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadImageW(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadIconW(IntPtr hInst, IntPtr lpIconName);

    [DllImport("user32")]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
