using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ContextMenuManager.Views;

/// <summary>
/// 全局对话框协调器。WinUI 同一时刻只允许存在一个 <see cref="ContentDialog"/>，
/// 并发弹框会抛 COMException(0x80000019) 直接崩溃整个应用。此处用单一静态守卫把
/// 所有页面的弹框串行化，替代此前分散在各页面里重复实现的 <c>_activeDialog</c> 字段。
/// </summary>
internal static class AppDialog
{
    private static ContentDialog? _active;

    /// <summary>当前是否已有对话框打开。</summary>
    public static bool IsOpen => _active != null;

    /// <summary>
    /// 串行化地显示一个对话框：若已有对话框打开则直接返回 <see cref="ContentDialogResult.None"/>，
    /// 并对极端并发下仍可能出现的 COMException 做降级（返回 None），保证不崩溃。
    /// </summary>
    public static async Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
    {
        if(_active != null) return ContentDialogResult.None;
        _active = dialog;
        try
        {
            return await dialog.ShowAsync();
        }
        catch(System.Runtime.InteropServices.COMException)
        {
            return ContentDialogResult.None;
        }
        finally
        {
            _active = null;
        }
    }

    /// <summary>显示一个"主要/取消"确认框，用户点击主要按钮时返回 true。</summary>
    public static async Task<bool> ConfirmAsync(XamlRoot root, string title, string message,
        string primaryText, string cancelText)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = root,
            Title = title,
            Content = message,
            PrimaryButtonText = primaryText,
            CloseButtonText = cancelText,
            DefaultButton = ContentDialogButton.Close
        };
        return await ShowAsync(dialog) == ContentDialogResult.Primary;
    }
}
