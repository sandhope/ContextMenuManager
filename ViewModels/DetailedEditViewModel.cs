#nullable disable
using ContextMenuManager.Core;
using ContextMenuManager.Localization;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ContextMenuManager.ViewModels;

/// <summary>详细编辑规则：通过注册表项存在性控制某项显隐</summary>
public class DetailedEditRule : ObservableObject
{
    public string Name { get; set; }
    public string RegPath { get; set; }
    public string Tip { get; set; }
    /// <summary>处理器 CLSID：启用时写回注册表项默认值以恢复功能；为空表示项名本身即 GUID，仅凭项存在性控制。</summary>
    public string Clsid { get; set; }
    public Action<string> StatusReporter { get; set; }

    private bool _isOn;
    public bool IsOn
    {
        get => _isOn;
        set { if(Set(ref _isOn, value)) Apply(); }
    }

    public void RefreshState()
    {
        bool exists;
        using(var key = RegistryEx.GetRegistryKey(RegPath)) exists = key != null;
        if(exists != _isOn)
        {
            _isOn = exists;
            OnPropertyChanged();
        }
    }

    private void Apply()
    {
        try
        {
            if(_isOn)
            {
                using(var key = RegistryEx.GetRegistryKey(RegPath, true, true))
                {
                    // 写回默认值 = 处理器 CLSID，保证禁用（删除项）后重新启用能恢复右键菜单处理器功能。
                    if(key != null && !string.IsNullOrEmpty(Clsid)) key.SetValue("", Clsid);
                }
            }
            else
            {
                RegistryEx.DeleteKeyTree(RegPath);
            }
            StatusReporter?.Invoke(AppStrings.Instance.Get(_isOn ? "Status.EnabledColon" : "Status.DisabledColon", Name));
        }
        catch(Exception ex)
        {
            // 还原开关状态
            _isOn = !_isOn;
            OnPropertyChanged();
            StatusReporter?.Invoke(AppStrings.Instance.Get("Status.OperationFailed", ex.Message));
        }
    }
}

/// <summary>详细编辑页面视图模型（简化版，实现真实可用的规则开关）</summary>
public class DetailedEditViewModel : ObservableObject
{
    public ObservableCollection<DetailedEditRule> Rules { get; } = new ObservableCollection<DetailedEditRule>();

    private string _statusText;
    public string StatusText { get => _statusText; set => Set(ref _statusText, value); }

    private ICommand _refreshCommand;
    public ICommand RefreshCommand => _refreshCommand ??= new RelayCommand(_ => Refresh());

    public DetailedEditViewModel()
    {
        string L(string key) => AppStrings.Instance.Get(key);

        void Add(string name, string regPath, string tip, string clsid = null)
        {
            Rules.Add(new DetailedEditRule
            {
                Name = name,
                RegPath = regPath,
                Tip = tip,
                Clsid = clsid,
                StatusReporter = s => StatusText = s
            });
        }

        const string HKCR = @"HKEY_CLASSES_ROOT";

        // 说明：ContextMenuHandlers 下真实项名多为友好名（如 Sharing/SendTo），CLSID 存于项默认值；
        // 少数项名本身即 GUID（默认值为空）。启用时写回默认值 = CLSID 才能保证 toggle 可逆。

        // 文件·共享（经典共享面板）
        Add(L("Detailed.ShareName"),
            $@"{HKCR}\*\shellex\ContextMenuHandlers\Sharing",
            L("Detailed.ShareTip"),
            "{f81e9010-6ea4-11ce-a7ff-00aa003ca9f6}");

        // 文件·现代共享（Win10/11 共享面板）
        Add(L("Detailed.ModernShareName"),
            $@"{HKCR}\*\shellex\ContextMenuHandlers\ModernSharing",
            L("Detailed.ModernShareTip"),
            "{e2bf9676-5f8f-435c-97eb-11607a5bedf7}");

        // 所有文件系统对象·发送到
        Add(L("Detailed.SendToName"),
            $@"{HKCR}\AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo",
            L("Detailed.SendToTip"),
            "{7BA4C740-9E81-11CF-99D3-00AA004AE837}");

        // 所有文件系统对象·还原以前的版本（项名本身即 GUID，默认值为空）
        Add(L("Detailed.PrevVersionsName"),
            $@"{HKCR}\AllFilesystemObjects\shellex\ContextMenuHandlers\{{596AB062-B4D2-4215-9F74-E9109B0A8153}}",
            L("Detailed.PrevVersionsTip"));

        // 目录·共享
        Add(L("Detailed.DirShareName"),
            $@"{HKCR}\Directory\shellex\ContextMenuHandlers\Sharing",
            L("Detailed.DirShareTip"),
            "{f81e9010-6ea4-11ce-a7ff-00aa003ca9f6}");

        // 文件夹·库位置
        Add(L("Detailed.LibraryLocationName"),
            $@"{HKCR}\Folder\shellex\ContextMenuHandlers\Library Location",
            L("Detailed.LibraryLocationTip"),
            "{3dad6c5d-2167-4cae-9914-f99e41c12cfa}");

        // 文件夹·固定到开始屏幕
        Add(L("Detailed.PinToStartName"),
            $@"{HKCR}\Folder\shellex\ContextMenuHandlers\PintoStartScreen",
            L("Detailed.PinToStartTip"),
            "{470C0EBD-5D73-4d58-9CED-E91E22E23282}");

        // 目录·打印（shell\print 存在即显示，无 CLSID）
        Add(L("Detailed.PrintDirName"),
            $@"{HKCR}\Directory\shell\print",
            L("Detailed.PrintDirTip"));

        foreach(var r in Rules) r.RefreshState();
        StatusText = AppStrings.Instance.Get("Status.CountRules", Rules.Count);
    }

    public void Refresh()
    {
        foreach(var r in Rules) r.RefreshState();
        StatusText = AppStrings.Instance.Get("Status.CountRules", Rules.Count);
    }
}
