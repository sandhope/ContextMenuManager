#nullable disable
using ContextMenuManager.Localization;
using ContextMenuManager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ContextMenuManager.Views;

public sealed partial class SendToPage : Page
{
    public SendToViewModel VM { get; }

    public SendToPage()
    {
        InitializeComponent();
        VM = new SendToViewModel();
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

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e) => VM.OpenFolder();
    private void RefreshButton_Click(object sender, RoutedEventArgs e) => VM.Load();
    private void RestoreButton_Click(object sender, RoutedEventArgs e) => VM.RestoreDefault();

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if((sender as FrameworkElement)?.DataContext is SendToItemViewModel vm)
            _ = ConfirmAndDelete(vm);
    }

    /// <summary>删除前弹确认框（删除不可撤销），避免误操作。</summary>
    private async System.Threading.Tasks.Task ConfirmAndDelete(SendToItemViewModel vm)
    {
        bool ok = await AppDialog.ConfirmAsync(
            XamlRoot,
            AppStrings.Instance.Get("Menu.Delete"),
            $"{AppStrings.Instance.Get("Message.ConfirmDeletePermanently")}\n\n{vm.DisplayName}",
            AppStrings.Instance.Get("Page.Delete"),
            AppStrings.Instance.Get("Menu.Cancel"));
        if(ok) VM.DeleteItem(vm);
    }
}
