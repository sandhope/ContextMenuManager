#nullable disable
using ContextMenuManager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ContextMenuManager.Views;

public sealed partial class DictionariesPage : Page
{
    public DictionariesViewModel VM { get; }

    public DictionariesPage()
    {
        InitializeComponent();
        VM = new DictionariesViewModel();
        VM.Load();
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e) => VM.OpenFolder();
    private void RefreshButton_Click(object sender, RoutedEventArgs e) => VM.Load();
}
