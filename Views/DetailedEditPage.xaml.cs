#nullable disable
using ContextMenuManager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ContextMenuManager.Views;

public sealed partial class DetailedEditPage : Page
{
    public DetailedEditViewModel VM { get; private set; }

    public DetailedEditPage() => InitializeComponent();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        VM = new DetailedEditViewModel();
        DataContext = VM;
        base.OnNavigatedTo(e);
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => VM.Refresh();
}
