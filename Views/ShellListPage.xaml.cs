#nullable disable
using ContextMenuManager.Core;
using ContextMenuManager.Localization;
using ContextMenuManager.Models;
using ContextMenuManager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ContextMenuManager.Views;

public sealed partial class ShellListPage : Page
{
    public ShellListViewModel VM { get; private set; }
    private ShellScenes.Scenes _scene;

    public ShellListPage() => InitializeComponent();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _scene = e.Parameter is ShellScenes.Scenes scene ? scene : ShellScenes.Scenes.File;
        VM = new ShellListViewModel(_scene);
        DataContext = VM;
        VM.RequestElevation += OnElevationRequested;
        VM.Items.CollectionChanged += (_, _) => UpdateEmptyState();
        ConfigureInputBar();
        ConfigureSceneDescription();
        VM.Load();
        UpdateEmptyState();
        base.OnNavigatedTo(e);
    }

    /// <summary>展示当前场景的一句话说明（取自 StatusBar.* 本地化键），缺失时隐藏。</summary>
    private void ConfigureSceneDescription()
    {
        // CommandStore 场景对应的说明键为 PublicReferences，其余场景与枚举名同名。
        string suffix = _scene == ShellScenes.Scenes.CommandStore ? "PublicReferences" : _scene.ToString();
        string key = "StatusBar." + suffix;
        string text = AppStrings.Instance.Get(key);
        if(string.IsNullOrEmpty(text) || text == key)
        {
            SceneDescription.Visibility = Visibility.Collapsed;
        }
        else
        {
            SceneDescription.Text = text;
            SceneDescription.Visibility = Visibility.Visible;
        }
    }

    /// <summary>根据当前项目数切换列表/空状态占位的可见性。</summary>
    private void UpdateEmptyState()
    {
        bool empty = VM == null || VM.Items.Count == 0;
        EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        ItemList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
        if(empty) EmptyStateText.Text = AppStrings.Instance.Get("Status.EmptyList");
    }

    /// <summary>按场景配置参数输入栏：参数化场景显示输入框/浏览按钮，其余场景隐藏。</summary>
    private void ConfigureInputBar()
    {
        var s = AppStrings.Instance;
        ApplyButton.Content = s.Get("Input.Apply");
        BrowseFileButton.Content = s.Get("Input.BrowseFile");
        BrowseFolderButton.Content = s.Get("Input.BrowseFolder");
        BrowseFileButton.Visibility = Visibility.Collapsed;
        BrowseFolderButton.Visibility = Visibility.Collapsed;
        ApplyButton.Visibility = Visibility.Visible;
        InputBox.IsReadOnly = false;

        switch(_scene)
        {
            case ShellScenes.Scenes.CustomExtension:
                InputLabel.Text = s.Get("Input.ExtensionLabel");
                InputBox.PlaceholderText = s.Get("Input.ExtensionPlaceholder");
                InputBox.Text = ShellScenes.Params.CurrentExtension ?? string.Empty;
                InputBar.Visibility = Visibility.Visible;
                break;
            case ShellScenes.Scenes.PerceivedType:
                InputLabel.Text = s.Get("Input.PerceivedTypeLabel");
                InputBox.PlaceholderText = s.Get("Input.PerceivedTypePlaceholder");
                InputBox.Text = ShellScenes.Params.CurrentPerceivedType ?? string.Empty;
                InputBar.Visibility = Visibility.Visible;
                break;
            case ShellScenes.Scenes.DirectoryType:
                InputLabel.Text = s.Get("Input.DirectoryTypeLabel");
                InputBox.PlaceholderText = s.Get("Input.DirectoryTypePlaceholder");
                InputBox.Text = ShellScenes.Params.CurrentDirectoryType ?? string.Empty;
                InputBar.Visibility = Visibility.Visible;
                break;
            case ShellScenes.Scenes.CustomRegPath:
                InputLabel.Text = s.Get("Input.CustomRegPathLabel");
                InputBox.PlaceholderText = s.Get("Input.CustomRegPathPlaceholder");
                InputBox.Text = ShellScenes.Params.CurrentCustomRegPath ?? string.Empty;
                InputBar.Visibility = Visibility.Visible;
                break;
            case ShellScenes.Scenes.MenuAnalysis:
                InputLabel.Text = s.Get("Input.AnalysisLabel");
                InputBox.PlaceholderText = s.Get("Input.AnalysisPlaceholder");
                InputBox.Text = ShellScenes.Params.CurrentFileObjectPath ?? string.Empty;
                InputBox.IsReadOnly = true;
                BrowseFileButton.Visibility = Visibility.Visible;
                BrowseFolderButton.Visibility = Visibility.Visible;
                ApplyButton.Visibility = Visibility.Collapsed;
                InputBar.Visibility = Visibility.Visible;
                break;
            default:
                InputBar.Visibility = Visibility.Collapsed;
                break;
        }
    }

    /// <summary>将输入框内容写回对应场景参数并重新枚举。</summary>
    private void ApplyInput()
    {
        string v = InputBox.Text?.Trim();
        switch(_scene)
        {
            case ShellScenes.Scenes.CustomExtension:
                if(!string.IsNullOrEmpty(v) && !v.StartsWith(".")) v = "." + v;
                ShellScenes.Params.CurrentExtension = string.IsNullOrEmpty(v) ? null : v;
                break;
            case ShellScenes.Scenes.PerceivedType:
                ShellScenes.Params.CurrentPerceivedType = string.IsNullOrEmpty(v) ? null : v;
                break;
            case ShellScenes.Scenes.DirectoryType:
                ShellScenes.Params.CurrentDirectoryType = string.IsNullOrEmpty(v) ? null : v;
                break;
            case ShellScenes.Scenes.CustomRegPath:
                ShellScenes.Params.CurrentCustomRegPath = string.IsNullOrEmpty(v) ? null : v;
                break;
            default:
                return;
        }
        VM.Load();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e) => ApplyInput();

    private void InputBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if(e.Key == Windows.System.VirtualKey.Enter && !InputBox.IsReadOnly) ApplyInput();
    }

    private async void BrowseFileButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        picker.FileTypeFilter.Add("*");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, App.MainWindowHandle);
        var file = await picker.PickSingleFileAsync();
        if(file != null) ApplyAnalysisPath(file.Path);
    }

    private async void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.FileTypeFilter.Add("*");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, App.MainWindowHandle);
        var folder = await picker.PickSingleFolderAsync();
        if(folder != null) ApplyAnalysisPath(folder.Path);
    }

    private void ApplyAnalysisPath(string path)
    {
        InputBox.Text = path;
        ShellScenes.Params.CurrentFileObjectPath = path;
        VM.Load();
    }

    private async void OnElevationRequested(object sender, string regPath)
    {
        // 如果已有 ContentDialog（如编辑框）打开，避免并发弹框导致 COMException。
        if(AppDialog.IsOpen)
        {
            VM.StatusText = AppStrings.Instance.Get("Dialog.ElevationPending") ?? "请先关闭当前对话框，再处理权限请求。";
            return;
        }

        bool isAdmin = Core.AdminHelper.IsRunningAsAdmin();
        // 已是管理员仍被拒：通常是键由系统(TrustedInstaller)拥有，连管理员也无写权限，再点"以管理员身份运行"无意义。
        string tip = isAdmin
            ? (AppStrings.Instance.Get("Dialog.SystemProtectedTip") ?? "此注册表项由系统(TrustedInstaller)拥有，即使是管理员也需取得其所有权后才能修改，当前版本暂不支持。")
            : (AppStrings.Instance.Get("Dialog.NeedAdminTip") ?? "修改或删除此注册表项需要管理员权限。");

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = AppStrings.Instance.Get("Dialog.NeedAdmin") ?? "需要管理员权限",
            Content = $"{tip}\n\n{regPath}",
            // 已是管理员则不再显示"以管理员身份运行"按钮（无效）
            PrimaryButtonText = isAdmin ? string.Empty : (AppStrings.Instance.Get("Dialog.RunAsAdmin") ?? "以管理员身份运行"),
            CloseButtonText = AppStrings.Instance.Get("Menu.Cancel") ?? "取消"
        };
        var result = await AppDialog.ShowAsync(dialog);
        if(result == ContentDialogResult.Primary && !isAdmin)
        {
            try
            {
                // 以 runas 直接拉起提权后的 exe，绕过 dotnet run 无法为非管理员终端提权子进程的限制。
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                    ?? Environment.ProcessPath;
                if(!string.IsNullOrEmpty(exePath))
                {
                    var psi = new System.Diagnostics.ProcessStartInfo(exePath)
                    {
                        Verb = "runas",
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                    // 启动成功后关闭当前非管理员实例，避免桌面上同时出现两个窗口。
                    Application.Current.Exit();
                }
            }
            catch { /* 用户取消 UAC 或启动失败，忽略 */ }
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e) => VM.AddItemCommand.Execute(null);
    private void RefreshButton_Click(object sender, RoutedEventArgs e) => VM.Load();

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if((sender as FrameworkElement)?.DataContext is MenuItemViewModel vm)
            _ = ShowEditDialog(vm);
    }

    private void DeleteMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        // 优先从 Tag 取（x:Bind 绑定到模板数据根，不依赖 Flyout 的 DataContext 传递）。
        var fe = sender as FrameworkElement;
        var vm = fe?.Tag as MenuItemViewModel ?? fe?.DataContext as MenuItemViewModel;
        if(vm != null) _ = ConfirmAndDelete(vm);
    }

    /// <summary>删除前弹确认框（删除不可撤销），避免误操作直接丢失菜单项。</summary>
    private async System.Threading.Tasks.Task ConfirmAndDelete(MenuItemViewModel vm)
    {
        bool ok = await AppDialog.ConfirmAsync(
            XamlRoot,
            AppStrings.Instance.Get("Menu.Delete"),
            $"{AppStrings.Instance.Get("Message.ConfirmDeletePermanently")}\n\n{vm.Text}",
            AppStrings.Instance.Get("Page.Delete"),
            AppStrings.Instance.Get("Menu.Cancel"));
        if(ok) vm.DeleteCommand.Execute(null);
    }

    private async System.Threading.Tasks.Task ShowEditDialog(MenuItemViewModel vm)
    {
        if(AppDialog.IsOpen) return;
        var textBox = new TextBox { Text = vm.Text, Header = AppStrings.Instance.Get("Dialog.ItemText"), Margin = new Thickness(0, 4, 0, 4) };
        var cmdBox = new TextBox { Text = vm.Command, Header = AppStrings.Instance.Get("Dialog.ItemCommand"), Margin = new Thickness(0, 4, 0, 4), AcceptsReturn = true };
        var panel = new StackPanel();
        panel.Children.Add(textBox);
        panel.Children.Add(cmdBox);

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = AppStrings.Instance.Get("Menu.Edit"),
            Content = panel,
            PrimaryButtonText = AppStrings.Instance.Get("Menu.Save"),
            CloseButtonText = AppStrings.Instance.Get("Menu.Cancel") ?? "取消"
        };
        if(await AppDialog.ShowAsync(dialog) == ContentDialogResult.Primary)
        {
            vm.Text = textBox.Text;
            vm.Command = cmdBox.Text;
        }
    }
}
