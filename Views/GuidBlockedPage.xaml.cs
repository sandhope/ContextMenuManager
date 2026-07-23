#nullable disable
using ContextMenuManager.Localization;
using ContextMenuManager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ContextMenuManager.Views;

public sealed partial class GuidBlockedPage : Page
{
    public GuidBlockedViewModel VM { get; private set; }

    public GuidBlockedPage() => InitializeComponent();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        VM = new GuidBlockedViewModel();
        VM.Items.CollectionChanged += (_, _) => UpdateEmptyState();
        DataContext = VM;
        UpdateEmptyState();
        base.OnNavigatedTo(e);
    }

    /// <summary>根据当前项目数切换列表/空状态占位的可见性。</summary>
    private void UpdateEmptyState()
    {
        bool empty = VM == null || VM.Items.Count == 0;
        EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        ItemList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => VM.Reload();
    private void Apply_Click(object sender, RoutedEventArgs e) => VM.ApplyLock();
    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if((sender as FrameworkElement)?.DataContext is GuidBlockedItem m)
            _ = ConfirmAndDelete(m);
    }

    /// <summary>删除前弹确认框（删除不可撤销），避免误操作。</summary>
    private async System.Threading.Tasks.Task ConfirmAndDelete(GuidBlockedItem m)
    {
        bool ok = await AppDialog.ConfirmAsync(
            XamlRoot,
            AppStrings.Instance.Get("Menu.Delete"),
            $"{AppStrings.Instance.Get("Message.ConfirmDeletePermanently")}\n\n{m.DisplayText}",
            AppStrings.Instance.Get("Page.Delete"),
            AppStrings.Instance.Get("Menu.Cancel"));
        if(ok) VM.DeleteItem(m);
    }
}
