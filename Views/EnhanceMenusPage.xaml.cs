#nullable disable
using ContextMenuManager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ContextMenuManager.Views;

public sealed partial class EnhanceMenusPage : Page
{
    public EnhanceMenusViewModel VM { get; }

    public EnhanceMenusPage()
    {
        InitializeComponent();
        VM = new EnhanceMenusViewModel();
        VM.SelectedScene = VM.Scenes[0]; // 触发 Load
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => VM.Load();

    private void Preset_Toggled(object sender, RoutedEventArgs e)
    {
        // ToggleSwitch 的 Toggled 在 x:Bind 初始化（IsOn 由 IsPresent 赋值）时也会触发，
        // 若此时直接调用 Toggle 会把"已存在"的增强项误删。仅当开关目标状态与当前实际状态不一致时才执行操作。
        if(sender is ToggleSwitch toggle && toggle.DataContext is EnhancePresetItem item)
        {
            if(toggle.IsOn != item.IsPresent) VM.Toggle(item);
        }
    }
}
