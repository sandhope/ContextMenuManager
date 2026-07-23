#nullable disable
using ContextMenuManager.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

namespace ContextMenuManager.Views;

public sealed partial class DonatePage : Page
{
    private const string RepoUrl = "https://github.com/sandhope/ContextMenuManager";

    public DonatePage() => InitializeComponent();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if(AppStrings.Instance.Language == AppLang.EnUs)
        {
            // 英文：捐赠页直接跳转当前仓库（同时保留可点击链接兜底）
            EnPanel.Visibility = Visibility.Visible;
            EnTip.Text = AppStrings.Instance.Get("Donate.EnTip");
            OpenRepository();
        }
        else
        {
            // 中文：展示两个收款码
            ZhPanel.Visibility = Visibility.Visible;
            ZhTitle.Text = AppStrings.Instance.Get("Donate.Title");
            AlipayLabel.Text = AppStrings.Instance.Get("Donate.Alipay");
            WechatLabel.Text = AppStrings.Instance.Get("Donate.WeChat");
            ZhTip.Text = AppStrings.Instance.Get("Donate.Tip");
            LoadImage(AlipayImage, "sponsor/alipay.jpg");
            LoadImage(WechatImage, "sponsor/weixin.jpg");
        }
    }

    /// <summary>从程序基目录加载本地图片（非打包全信任应用直接用 file:// URI）。</summary>
    private static void LoadImage(Image image, string relativePath)
    {
        try
        {
            string full = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
            if(!File.Exists(full)) return;
            image.Source = new BitmapImage(new Uri(full));
        }
        catch { /* 图片缺失或加载失败则留空，不影响其余界面 */ }
    }

    /// <summary>以默认浏览器打开当前仓库（英文捐赠页的"直接跳转"）。</summary>
    private static void OpenRepository()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(RepoUrl) { UseShellExecute = true };
            System.Diagnostics.Process.Start(psi);
        }
        catch { /* 浏览器拉起失败则依赖页面上的链接兜底 */ }
    }
}
