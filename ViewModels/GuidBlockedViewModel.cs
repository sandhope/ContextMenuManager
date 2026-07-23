#nullable disable
using ContextMenuManager.Core;
using ContextMenuManager.Localization;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace ContextMenuManager.ViewModels;

/// <summary>被锁定的 GUID 项</summary>
public class GuidBlockedItem : ObservableObject
{
    public Guid Guid { get; set; }
    public string GuidText => Guid.ToString("B");
    public string DisplayText => GuidInfo.GetText(Guid) ?? AppStrings.Instance.Get("Status.Unknown");
}

/// <summary>
/// GUID 锁页面视图模型。
/// 在本工程自己的注册表位置维护被锁 GUID 列表（REG_MULTI_SZ），
/// "应用锁定"会把已知 ShellEx 处理器中默认值等于该 GUID 的项移入 Backup 子项（不真正删除，便于还原）。
/// </summary>
public class GuidBlockedViewModel : ObservableObject
{
    public const string StorePath = @"HKEY_CURRENT_USER\Software\ContextMenuManager\GuidBlocked";
    public const string ValueName = "BlockedGuids";

    // 需要遍历的 ShellEx 场景与处理器类型
    private static readonly string[] ScenePaths =
    {
        ShellScenes.MENUPATH_FOLDER,
        ShellScenes.MENUPATH_DIRECTORY,
        ShellScenes.MENUPATH_DRIVE,
        ShellScenes.MENUPATH_ALLOBJECTS,
    };
    private static readonly string[] HandlerTypes = { "ContextMenuHandlers", "DragDropHandlers" };

    public ObservableCollection<GuidBlockedItem> Items { get; } = new ObservableCollection<GuidBlockedItem>();

    private GuidBlockedItem _selectedItem;
    public GuidBlockedItem SelectedItem { get => _selectedItem; set => Set(ref _selectedItem, value); }

    private string _newGuid;
    public string NewGuid { get => _newGuid; set => Set(ref _newGuid, value); }

    private string _statusText;
    public string StatusText { get => _statusText; set => Set(ref _statusText, value); }

    private ICommand _addCommand;
    public ICommand AddCommand => _addCommand ??= new RelayCommand(_ => AddGuid());
    private ICommand _applyCommand;
    public ICommand ApplyCommand => _applyCommand ??= new RelayCommand(_ => ApplyLock());
    private ICommand _refreshCommand;
    public ICommand RefreshCommand => _refreshCommand ??= new RelayCommand(_ => Reload());

    private readonly List<Guid> _guids = new List<Guid>();

    public GuidBlockedViewModel()
    {
        // 构造时加载：注册表环境异常不得拖垮页面导航，失败时降级为状态提示。
        try { Reload(); }
        catch(Exception ex) { StatusText = AppStrings.Instance.Get("Status.LoadFailed", ex.Message); }
    }

    public void Reload()
    {
        _guids.Clear();
        var value = Registry.GetValue(StorePath, ValueName, null) as string[];
        if(value != null)
        {
            foreach(string s in value)
            {
                if(GuidEx.TryParse(s, out Guid g) && !_guids.Any(x => x.Equals(g)))
                    _guids.Add(g);
            }
        }
        Items.Clear();
        foreach(var g in _guids) Items.Add(new GuidBlockedItem { Guid = g });
        StatusText = AppStrings.Instance.Get("Status.CountLockedGuid", Items.Count);
    }

    private void Save()
    {
        try
        {
            string[] arr = _guids.Select(g => g.ToString("B")).ToArray();
            if(arr.Length == 0) RegistryEx.DeleteValue(StorePath, ValueName);
            else Registry.SetValue(StorePath, ValueName, arr);
        }
        catch(Exception ex)
        {
            StatusText = AppStrings.Instance.Get("Status.SaveFailed", ex.Message);
        }
    }

    public void AddGuid()
    {
        if(string.IsNullOrWhiteSpace(NewGuid)) { StatusText = AppStrings.Instance.Get("Status.InputGuidPrompt"); return; }
        if(!GuidEx.TryParse(NewGuid, out Guid g)) { StatusText = AppStrings.Instance.Get("Status.GuidInvalid"); return; }
        if(_guids.Any(x => x.Equals(g))) { StatusText = AppStrings.Instance.Get("Status.GuidAlreadyLocked"); return; }
        _guids.Add(g);
        NewGuid = "";
        Save();
        Reload();
        StatusText = AppStrings.Instance.Get("Status.LockAdded", g.ToString("B"));
    }

    public void DeleteItem(GuidBlockedItem item)
    {
        if(item == null) return;
        _guids.Remove(item.Guid);
        Save();
        Reload();
        StatusText = AppStrings.Instance.Get("Status.Removed");
    }

    public void ApplyLock()
    {
        try
        {
            int moved = 0;
            foreach(var g in _guids)
            {
                string target = g.ToString("B");
                foreach(string scenePath in ScenePaths)
                {
                    foreach(string handlerType in HandlerTypes)
                    {
                        string basePath = $@"{scenePath}\ShellEx\{handlerType}";
                        using(RegistryKey baseKey = RegistryEx.GetRegistryKey(basePath, true))
                        {
                            if(baseKey == null) continue;
                            foreach(string name in baseKey.GetSubKeyNames())
                            {
                                string subPath = $@"{basePath}\{name}";
                                string val = Registry.GetValue(subPath, "", null) as string;
                                if(val != null && val.Equals(target, StringComparison.OrdinalIgnoreCase))
                                {
                                    string backupPath = $@"{basePath}\__BlockedBackup";
                                    using(RegistryKey backupKey = RegistryEx.GetRegistryKey(backupPath, true, true))
                                    using(RegistryKey subKey = RegistryEx.GetRegistryKey(subPath, false))
                                    {
                                        if(subKey != null)
                                        {
                                            subKey.MoveTo(backupKey);
                                            moved++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            StatusText = moved > 0 ? AppStrings.Instance.Get("Status.HiddenCount", moved) : AppStrings.Instance.Get("Status.NoMatchShellEx");
        }
        catch(Exception ex)
        {
            StatusText = AppStrings.Instance.Get("Status.ApplyFailed", ex.Message);
        }
    }
}
