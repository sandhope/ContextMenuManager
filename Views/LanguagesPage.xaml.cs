using ContextMenuManager.Core;
using ContextMenuManager.Localization;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace ContextMenuManager.Views;

public sealed partial class LanguagesPage : Page
{
    // 与 LangList.Items 一一对应的语言 id（全部来自代码内置语言表）
    private readonly List<string> _langIds = new();
    private bool _initializing = true;

    public LanguagesPage()
    {
        InitializeComponent();

        // 单选按钮组：每个内置语言一项，显示其本地名称（如「简体中文」「English」）。
        foreach((string id, string display) in AppStrings.BuiltInLanguages)
        {
            LangList.Items.Add(display);
            _langIds.Add(id);
        }

        // 高亮当前生效语言（缺省或未匹配时选中第一项）；初始化阶段不触发切换。
        string current = AppConfig.Language;
        int index = string.IsNullOrWhiteSpace(current) ? 0 : _langIds.FindIndex(
            id => id.Equals(current, StringComparison.OrdinalIgnoreCase));
        LangList.SelectedIndex = index < 0 ? 0 : index;
        _initializing = false;
    }

    private void LangList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if(_initializing) return;
        int index = LangList.SelectedIndex;
        if(index < 0 || index >= _langIds.Count) return;

        string langId = _langIds[index];
        // 若选中项已是当前生效语言则直接返回：RadioButtons 在加载后可能异步再次
        // 抛出 SelectionChanged，而 LoadLanguage 会同步触发 MainWindow 重建并重新导航回本页、
        // 新页又会程序化选中当前语言——若不做等值判断会形成递归回环，
        // 导致页面疯狂抖动直至卡死崩溃。与“当前实际生效语言”（AppStrings.Language，
        // 总为确定值）比较，既不依赖事件时机，也避免首次运行配置为空时的多余重建。
        AppLang target = string.Equals(langId, "en-US", StringComparison.OrdinalIgnoreCase)
            ? AppLang.EnUs : AppLang.ZhCn;
        if(target == AppStrings.Instance.Language) return;
        // 必须先持久化再触发切换：LoadLanguage 会同步引发 MainWindow 重建导航并重新导航到
        // 本页，新页构造时会读取 AppConfig.Language 决定高亮项。若顺序颠倒，新页会读到旧值，
        // 导致「界面已是新语言、列表却高亮旧语言」的不一致。
        AppConfig.Language = langId;               // 持久化，下次启动自动加载
        AppStrings.Instance.LoadLanguage(langId); // 实时刷新（MainWindow 监听 Version 变化重建界面）
    }
}
