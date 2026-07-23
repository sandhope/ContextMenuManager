#nullable disable
using ContextMenuManager.Localization;
using Microsoft.UI.Xaml.Controls;

namespace ContextMenuManager.Views;

public sealed partial class AboutPage : Page
{
    public string About => AppStrings.Instance.Get("AboutApp.Text");

    public AboutPage() => InitializeComponent();
}
