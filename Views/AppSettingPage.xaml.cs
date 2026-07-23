#nullable disable
using ContextMenuManager.Localization;
using ContextMenuManager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace ContextMenuManager.Views;

public sealed partial class AppSettingPage : Page
{
    public AppSettingsViewModel VM { get; } = new AppSettingsViewModel();

    // 更新频率选项：文本走 i18n（Other.OnceAWeek 等），语言切换后页面重建会重新取值。
    private readonly List<(string Name, int Days)> _freqOptions = new()
    {
        (AppStrings.Instance.Get("Other.OnceAWeek"), 7),
        (AppStrings.Instance.Get("Other.OnceAMonth"), 30),
        (AppStrings.Instance.Get("Other.OnceASeason"), 90),
        (AppStrings.Instance.Get("Other.NeverCheck"), -1)
    };

    public AppSettingPage()
    {
        InitializeComponent();
        DataContext = VM;
        foreach(var opt in _freqOptions)
            FreqCombo.Items.Add(opt.Name);
        int idx = _freqOptions.FindIndex(o => o.Days == VM.UpdateFrequency);
        FreqCombo.SelectedIndex = idx >= 0 ? idx : 1;
    }

    private void FreqCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int i = FreqCombo.SelectedIndex;
        if(i >= 0) VM.UpdateFrequency = _freqOptions[i].Days;
    }
}
