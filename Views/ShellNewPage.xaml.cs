#nullable disable
using ContextMenuManager.Localization;
using ContextMenuManager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ContextMenuManager.Views;

public sealed partial class ShellNewPage : Page
{
    public ShellNewViewModel VM { get; }

    public ShellNewPage()
    {
        InitializeComponent();
        VM = new ShellNewViewModel();
        VM.Items.CollectionChanged += (_, _) => UpdateEmptyState();
        VM.Load();
        UpdateEmptyState();
    }

    /// <summary>根据当前项目数切换列表/空状态占位的可见性。</summary>
    private void UpdateEmptyState()
    {
        bool empty = VM == null || VM.Items.Count == 0;
        EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        ItemList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e) => ShowAddDialog();

    private async void ShowAddDialog()
    {
        if(AppDialog.IsOpen) return;
        var textBox = new TextBox
        {
            Header = AppStrings.Instance.Get("Dialog.ShellNewExtension") ?? "扩展名（如 txt、bmp，文件夹为 Folder）",
            PlaceholderText = "txt",
            Margin = new Thickness(0, 4, 0, 4)
        };
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = AppStrings.Instance.Get("Dialog.ShellNewAddTitle") ?? "添加新建菜单项",
            Content = textBox,
            PrimaryButtonText = AppStrings.Instance.Get("Page.Add") ?? "添加",
            CloseButtonText = AppStrings.Instance.Get("Menu.Cancel") ?? "取消"
        };
        if(await AppDialog.ShowAsync(dialog) == ContentDialogResult.Primary)
            VM.AddItem(textBox.Text);
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => VM.Load();

    private void RegeditButton_Click(object sender, RoutedEventArgs e) => VM.OpenInRegedit();

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if((sender as FrameworkElement)?.DataContext is ShellNewItemViewModel vm)
            _ = ConfirmAndDelete(vm);
    }

    /// <summary>删除前弹确认框（删除不可撤销），避免误操作。</summary>
    private async System.Threading.Tasks.Task ConfirmAndDelete(ShellNewItemViewModel vm)
    {
        bool ok = await AppDialog.ConfirmAsync(
            XamlRoot,
            AppStrings.Instance.Get("Menu.Delete"),
            $"{AppStrings.Instance.Get("Message.ConfirmDeletePermanently")}\n\n{vm.Extension}",
            AppStrings.Instance.Get("Page.Delete"),
            AppStrings.Instance.Get("Menu.Cancel"));
        if(ok) VM.DeleteItem(vm);
    }

    private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if((sender as FrameworkElement)?.DataContext is ShellNewItemViewModel vm)
            VM.ToggleItem(vm, vm.IsEnabled);
    }
}
