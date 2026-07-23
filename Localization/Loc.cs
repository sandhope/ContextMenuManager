#nullable disable
namespace ContextMenuManager.Localization;

/// <summary>
/// 供 XAML x:Bind 函数绑定使用的本地化取值助手，
/// 用法：Label="{x:Bind loc:Loc.Tr('ToolBar.Refresh')}"。
/// 绑定为 OneTime；语言切换时由 MainWindow 重新导航（重建页面）实现实时刷新。
/// </summary>
public static class Loc
{
    public static string Tr(string key) => AppStrings.Instance.Get(key);
}
