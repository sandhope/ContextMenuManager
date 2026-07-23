#nullable disable
using ContextMenuManager.Core;
using ContextMenuManager.Localization;
using ContextMenuManager.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using WinRT.Interop;

namespace ContextMenuManager;

public sealed partial class MainWindow : Window, IDisposable
{
    /// <summary>导航项描述</summary>
    private sealed class NavItem
    {
        public string TitleKey { get; set; }
        public Type PageType { get; set; }
        public object Parameter { get; set; }
        public string Glyph { get; set; }
    }

    private readonly List<(string GroupKey, List<NavItem> Items)> _navGroups = new();
    private List<object> _menuItems = new();
    /// <summary>扫平的“标题 → 导航项”索引，供搜索框模糊匹配与定位。</summary>
    private readonly List<(string Title, NavItem Item)> _searchIndex = new();
    private NavItem _currentNavItem;
    private readonly TrayIconManager _tray;

    public MainWindow()
    {
        App.Trace("MainWindow.ctor enter");
        InitializeComponent();
        App.Trace("MainWindow.InitializeComponent done");
        BuildNavModel();
        App.Trace("MainWindow.BuildNavModel done");
        TrySetInitialSize();
        TrySetTaskbarIcon();
        _tray = new TrayIconManager(this);
        // 窗口关闭(X)→直接退出：先清理托盘图标再让进程退出。
        this.Closed += (_, _) => Dispose();
        // 必须在窗口 Activate 之后（消息循环已运转）再注册托盘，否则 Shell_NotifyIcon 常静默失败。
        this.Activated += OnMainWindowActivated;
    }

    private void OnMainWindowActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs e)
    {
        // 仅初始化一次：首次获得焦点（即 Activate 后）即注册托盘。
        this.Activated -= OnMainWindowActivated;
        _tray.Initialize("Windows 右键管理");
    }

    /// <summary>释放托盘图标等原生资源（满足 CA1001；实际清理由窗口 Closed 触发）。</summary>
    public void Dispose()
    {
        _tray?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>为任务栏按钮设置窗口图标：WinUI 3 不会自动使用 Assets\AppIcon.ico，
    /// 必须显式调用 AppWindow.SetIcon（参考 WinUI-Gallery 写法，这里用绝对路径更稳妥）。</summary>
    private void TrySetTaskbarIcon()
    {
        try
        {
            var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (System.IO.File.Exists(iconPath))
                AppWindow.SetIcon(iconPath);
        }
        catch (Exception ex)
        {
            App.Trace($"SetTaskbarIcon failed: {ex.Message}");
        }
    }

    /// <summary>设置一个适合多项列表的默认窗口尺寸（失败不影响启动）。</summary>
    private void TrySetInitialSize()
    {
        try { AppWindow?.Resize(new Windows.Graphics.SizeInt32(1100, 720)); }
        catch { /* 部分环境获取 AppWindow 失败时忽略，使用系统默认尺寸 */ }
    }

    private void BuildNavModel()
    {
        var s = AppStrings.Instance;

        var home = new List<NavItem>
        {
            new NavItem { TitleKey = "SideBar.File", Glyph = "\uE8A5", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.File },
            new NavItem { TitleKey = "SideBar.Folder", Glyph = "\uE8B7", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.Folder },
            new NavItem { TitleKey = "SideBar.Directory", Glyph = "\uE838", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.Directory },
            new NavItem { TitleKey = "SideBar.Background", Glyph = "\uE91B", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.Background },
            new NavItem { TitleKey = "SideBar.Desktop", Glyph = "\uE7F4", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.Desktop },
            new NavItem { TitleKey = "SideBar.Drive", Glyph = "\uEDA2", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.Drive },
            new NavItem { TitleKey = "SideBar.AllObjects", Glyph = "\uE8A9", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.AllObjects },
            new NavItem { TitleKey = "SideBar.Computer", Glyph = "\uE977", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.Computer },
            new NavItem { TitleKey = "SideBar.RecycleBin", Glyph = "\uE74D", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.RecycleBin },
            new NavItem { TitleKey = "SideBar.Library", Glyph = "\uE8F1", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.Library },
            new NavItem { TitleKey = "SideBar.New", Glyph = "\uE710", PageType = typeof(ShellNewPage) },
            new NavItem { TitleKey = "SideBar.SendTo", Glyph = "\uE724", PageType = typeof(SendToPage) },
            new NavItem { TitleKey = "SideBar.OpenWith", Glyph = "\uE8A7", PageType = typeof(OpenWithPage) },
            new NavItem { TitleKey = "SideBar.WinX", Glyph = "\uE71D", PageType = typeof(WinXPage) },
        };

        var type = new List<NavItem>
        {
            new NavItem { TitleKey = "SideBar.LnkFile", Glyph = "\uE71B", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.LnkFile },
            new NavItem { TitleKey = "SideBar.UwpLnk", Glyph = "\uE71B", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.UwpLnk },
            new NavItem { TitleKey = "SideBar.ExeFile", Glyph = "\uE756", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.ExeFile },
            new NavItem { TitleKey = "SideBar.CustomExtension", Glyph = "\uE71C", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.CustomExtension },
            new NavItem { TitleKey = "SideBar.PerceivedType", Glyph = "\uE8A1", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.PerceivedType },
            new NavItem { TitleKey = "SideBar.DirectoryType", Glyph = "\uE8B7", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.DirectoryType },
            new NavItem { TitleKey = "SideBar.UnknownType", Glyph = "\uE9CE", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.UnknownType },
            new NavItem { TitleKey = "SideBar.MenuAnalysis", Glyph = "\uE9D9", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.MenuAnalysis },
        };

        var rule = new List<NavItem>
        {
            new NavItem { TitleKey = "SideBar.EnhanceMenu", Glyph = "\uE90F", PageType = typeof(EnhanceMenusPage) },
            new NavItem { TitleKey = "SideBar.DetailedEdit", Glyph = "\uE70F", PageType = typeof(DetailedEditPage) },
            new NavItem { TitleKey = "SideBar.DragDrop", Glyph = "\uE7C2", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.DragDrop },
            new NavItem { TitleKey = "SideBar.PublicReferences", Glyph = "\uE8FD", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.CommandStore },
            new NavItem { TitleKey = "SideBar.CustomRegPath", Glyph = "\uE7C3", PageType = typeof(ShellListPage), Parameter = ShellScenes.Scenes.CustomRegPath },
            new NavItem { TitleKey = "SideBar.GuidBlocked", Glyph = "\uE711", PageType = typeof(GuidBlockedPage) },
        };

        var about = new List<NavItem>
        {
            new NavItem { TitleKey = "SideBar.AppSetting", Glyph = "\uE713", PageType = typeof(AppSettingPage) },
            new NavItem { TitleKey = "SideBar.AppLanguage", Glyph = "\uE774", PageType = typeof(LanguagesPage) },
            new NavItem { TitleKey = "SideBar.Dictionaries", Glyph = "\uE8FD", PageType = typeof(DictionariesPage) },
            new NavItem { TitleKey = "SideBar.AboutApp", Glyph = "\uE946", PageType = typeof(AboutPage) },
            new NavItem { TitleKey = "SideBar.Donate", Glyph = "\uE734", PageType = typeof(DonatePage) },
        };

        _navGroups.Add(("ToolBar.Home", home));
        _navGroups.Add(("ToolBar.Type", type));
        _navGroups.Add(("ToolBar.Rule", rule));
        _navGroups.Add(("ToolBar.About", about));
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        Title = AppStrings.Instance.Get("Page.AppTitle");
        NavView.PaneTitle = AppStrings.Instance.Get("Page.AppTitle");
        NavSearchBox.PlaceholderText = AppStrings.Instance.Get("Page.Search");
        BuildMenuItems();
        AppStrings.Instance.PropertyChanged += OnStringsChanged;
        // 默认选中第一个可导航项 → 触发 SelectionChanged 自动导航（修复首屏无数据）
        SelectNavItem(_currentNavItem);
    }

    /// <summary>根据当前语言重建导航菜单（含分组标题、图标与本地化文本）</summary>
    private void BuildMenuItems()
    {
        var items = new List<object>();
        _searchIndex.Clear();
        foreach(var group in _navGroups)
        {
            items.Add(new NavigationViewItemHeader { Content = AppStrings.Instance.Get(group.GroupKey) });
            foreach(var item in group.Items)
            {
                string title = AppStrings.Instance.Get(item.TitleKey);
                items.Add(new NavigationViewItem
                {
                    Content = title,
                    Icon = new FontIcon { Glyph = item.Glyph },
                    Tag = item
                });
                _searchIndex.Add((title, item));
            }
        }
        _menuItems = items;
        NavView.MenuItemsSource = items;
    }

    /// <summary>搜索框输入时提供匹配菜单标题建议（仅响应用户输入）。</summary>
    private void NavSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if(args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
        string q = sender.Text?.Trim() ?? string.Empty;
        sender.ItemsSource = string.IsNullOrEmpty(q)
            ? new List<string>()
            : _searchIndex
                .Where(t => t.Title.Contains(q, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.Title)
                .ToList();
    }

    /// <summary>选中建议项时回填输入框文本。</summary>
    private void NavSearch_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if(args.SelectedItem is string s) sender.Text = s;
    }

    /// <summary>提交搜索（回车或选定建议）：导航到标题完全匹配或首个前缀匹配的菜单项。</summary>
    private void NavSearch_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        string q = (args.ChosenSuggestion as string ?? args.QueryText)?.Trim();
        if(string.IsNullOrEmpty(q)) return;
        var hit = _searchIndex.FirstOrDefault(t => string.Equals(t.Title, q, StringComparison.OrdinalIgnoreCase));
        hit.Item ??= _searchIndex
            .FirstOrDefault(t => t.Title.Contains(q, StringComparison.OrdinalIgnoreCase)).Item;
        if(hit.Item != null) SelectNavItem(hit.Item);
    }

    /// <summary>选中与指定逻辑导航项对应的新容器；为空或未找到时选中第一个可导航项。</summary>
    private void SelectNavItem(NavItem target)
    {
        NavigationViewItem first = null;
        foreach(var obj in _menuItems)
        {
            if(obj is NavigationViewItem nvi)
            {
                first ??= nvi;
                if(target != null && ReferenceEquals(nvi.Tag, target))
                {
                    NavView.SelectedItem = nvi;
                    return;
                }
            }
        }
        if(first != null) NavView.SelectedItem = first;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if(args.SelectedItem is NavigationViewItem nvi && nvi.Tag is NavItem item)
        {
            _currentNavItem = item;
            NavView.Header = AppStrings.Instance.Get(item.TitleKey);
            // 先记录当前导航上下文：若页面构造/OnNavigatedTo 抛异常，未处理异常处理器可据此定位出错页面。
            App.CurrentContext = $"页面 {item.PageType.Name} ({item.TitleKey})";
            ContentFrame.Navigate(item.PageType, item.Parameter);
        }
    }

    /// <summary>语言切换（AppStrings.Version 变化）时实时刷新：重建菜单、更新标题，
    /// 并重新选中当前项 → 重新导航使页面内 x:Bind 文本重新取值为新语言。</summary>
    private void OnStringsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if(e.PropertyName != nameof(AppStrings.Version)) return;
        var current = _currentNavItem;
        Title = AppStrings.Instance.Get("Page.AppTitle");
        NavView.PaneTitle = AppStrings.Instance.Get("Page.AppTitle");
        NavSearchBox.PlaceholderText = AppStrings.Instance.Get("Page.Search");
        BuildMenuItems();
        SelectNavItem(current);
    }
}
