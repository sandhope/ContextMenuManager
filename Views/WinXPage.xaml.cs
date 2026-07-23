#nullable disable
using ContextMenuManager.Core;
using ContextMenuManager.Localization;
using ContextMenuManager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ContextMenuManager.Views;

public sealed partial class WinXPage : Page
{
    public WinXViewModel VM { get; }

    // 当前已订阅 Children 变化的分组，切换分组时需先解除旧订阅。
    private WinXGroupNode _watchedGroup;

    public WinXPage()
    {
        InitializeComponent();
        VM = new WinXViewModel();
        VM.PropertyChanged += VM_PropertyChanged;
        VM.Load();
        UpdateEmptyState();
    }

    private void VM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if(e.PropertyName == nameof(WinXViewModel.SelectedGroup))
        {
            if(_watchedGroup != null) _watchedGroup.Children.CollectionChanged -= Children_CollectionChanged;
            _watchedGroup = VM.SelectedGroup;
            if(_watchedGroup != null) _watchedGroup.Children.CollectionChanged += Children_CollectionChanged;
            UpdateEmptyState();
        }
    }

    private void Children_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => UpdateEmptyState();

    /// <summary>根据当前分组的项目数切换列表/空状态占位的可见性。</summary>
    private void UpdateEmptyState()
    {
        bool empty = VM?.SelectedGroup == null || VM.SelectedGroup.Children.Count == 0;
        EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        ItemList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e) => VM.OpenFolder();
    private void RefreshButton_Click(object sender, RoutedEventArgs e) => VM.Load();
    private void RestoreButton_Click(object sender, RoutedEventArgs e) => VM.RestoreDefault();
    private void NewGroup_Click(object sender, RoutedEventArgs e) => VM.CreateGroup();

    private async void AddItem_Click(object sender, RoutedEventArgs e)
    {
        if(AppDialog.IsOpen) return;
        var groupNames = VM.GetGroupNames();
        if(groupNames.Count == 0) { VM.StatusText = AppStrings.Instance.Get("Dialog.NoGroupTip") ?? "没有可用分组，请先新建分组"; return; }

        var targetBox = new TextBox { Header = AppStrings.Instance.Get("Dialog.WinXTargetPath") ?? "目标程序路径", PlaceholderText = @"C:\Windows\System32\cmd.exe", Margin = new Thickness(0, 4, 0, 4) };
        var browseBtn = new Button { Content = AppStrings.Instance.Get("Dialog.Browse") ?? "浏览", Margin = new Thickness(0, 0, 0, 4), HorizontalAlignment = HorizontalAlignment.Left };
        var argBox = new TextBox { Header = AppStrings.Instance.Get("Dialog.WinXArguments") ?? "参数（可选）", PlaceholderText = "%V", Margin = new Thickness(0, 4, 0, 4) };
        var groupCombo = new ComboBox { Header = AppStrings.Instance.Get("Dialog.WinXGroup") ?? "分组", ItemsSource = groupNames, SelectedIndex = 0, Margin = new Thickness(0, 4, 0, 4), MinWidth = 260 };
        var textBox = new TextBox { Header = AppStrings.Instance.Get("Dialog.WinXDisplayText") ?? "显示文本（可选，留空则用程序名）", Margin = new Thickness(0, 4, 0, 4) };

        browseBtn.Click += async (s, ev) =>
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            InitializeWithWindow.Initialize(picker, App.MainWindowHandle);
            var file = await picker.PickSingleFileAsync();
            if(file != null) targetBox.Text = file.Path;
        };

        var panel = new StackPanel();
        panel.Children.Add(targetBox);
        panel.Children.Add(browseBtn);
        panel.Children.Add(argBox);
        panel.Children.Add(groupCombo);
        panel.Children.Add(textBox);

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = AppStrings.Instance.Get("Dialog.WinXAddTitle") ?? "添加 WinX 项",
            Content = panel,
            PrimaryButtonText = AppStrings.Instance.Get("Page.Add") ?? "添加",
            CloseButtonText = AppStrings.Instance.Get("Menu.Cancel") ?? "取消"
        };
        if(await AppDialog.ShowAsync(dialog) == ContentDialogResult.Primary)
        {
            string group = groupCombo.SelectedItem as string;
            VM.AddItem(targetBox.Text, argBox.Text, group, textBox.Text);
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if((sender as FrameworkElement)?.DataContext is WinXItemNode vm)
            _ = ConfirmAndDelete(vm);
    }

    /// <summary>删除前弹确认框（删除不可撤销），避免误操作。</summary>
    private async System.Threading.Tasks.Task ConfirmAndDelete(WinXItemNode vm)
    {
        bool ok = await AppDialog.ConfirmAsync(
            XamlRoot,
            AppStrings.Instance.Get("Menu.Delete"),
            $"{AppStrings.Instance.Get("Message.ConfirmDeletePermanently")}\n\n{vm.Name}",
            AppStrings.Instance.Get("Page.Delete"),
            AppStrings.Instance.Get("Menu.Cancel"));
        if(ok) VM.DeleteItem(vm);
    }
}
