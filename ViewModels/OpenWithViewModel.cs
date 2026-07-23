#nullable disable
using ContextMenuManager.Core;
using ContextMenuManager.Localization;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ContextMenuManager.ViewModels
{
    /// <summary>打开方式（OpenWith）视图模型：加载 HKCR\Applications 下所有可作为打开方式的程序</summary>
    public class OpenWithViewModel : ObservableObject
    {
        private string _statusText = string.Empty;

        public ObservableCollection<OpenWithItemViewModel> Items { get; } = new();

        public string StatusText
        {
            get => _statusText;
            set => Set(ref _statusText, value);
        }

        public void Load()
        {
            try
            {
                Items.Clear();
                using(RegistryKey root = Registry.ClassesRoot)
                using(RegistryKey appKey = root.OpenSubKey("Applications"))
                {
                    if(appKey == null) { StatusText = AppStrings.Instance.Get("Status.NotFoundApplications"); return; }
                    foreach(string appName in appKey.GetSubKeyNames())
                    {
                        if(!appName.Contains('.')) continue; // 需要为有扩展名的文件名
                        using(RegistryKey shellKey = appKey.OpenSubKey($@"{appName}\shell"))
                        {
                            if(shellKey == null) continue;

                            var names = shellKey.GetSubKeyNames().ToList();
                            // 优先使用 open 动词：将已存在的 open 项移到首位（保留原大小写，避免重复插入）。
                            int openIdx = names.FindIndex(n => string.Equals(n, "open", StringComparison.OrdinalIgnoreCase));
                            if(openIdx > 0)
                            {
                                string openName = names[openIdx];
                                names.RemoveAt(openIdx);
                                names.Insert(0, openName);
                            }

                            string keyName = names.Find(name =>
                            {
                                using(RegistryKey cmdKey = shellKey.OpenSubKey(name))
                                    return cmdKey != null && cmdKey.GetValue("NeverDefault") == null;
                            });
                            if(keyName == null) continue;

                            using(RegistryKey commandKey = shellKey.OpenSubKey($@"{keyName}\command"))
                            {
                                string command = commandKey?.GetValue("")?.ToString();
                                string filePath = ObjectPath.ExtractFilePath(command);
                                if(string.IsNullOrEmpty(filePath)) continue;

                                string regPath = commandKey.Name; // HKCR\Applications\xxx\shell\open\command
                                string appPath = $@"HKEY_CLASSES_ROOT\Applications\{appName}";
                                Items.Add(new OpenWithItemViewModel(regPath, appPath, filePath));
                            }
                        }
                    }
                }

                var sorted = Items.OrderBy(i => i.DisplayText, StringComparer.CurrentCulture).ToList();
                Items.Clear();
                foreach(var it in sorted) Items.Add(it);

                StatusText = AppStrings.Instance.Get("Status.CountPrograms", Items.Count);
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.LoadFailed", ex.Message);
            }
        }
    }

    /// <summary>单个打开方式程序项</summary>
    public class OpenWithItemViewModel : ObservableObject
    {
        public OpenWithItemViewModel(string regPath, string appPath, string filePath)
        {
            RegPath = regPath;
            AppPath = appPath;
            ItemFilePath = filePath;
            DisplayText = GetDisplayName(appPath, filePath);
        }

        public string RegPath { get; }
        public string AppPath { get; }
        public string ItemFilePath { get; }
        public string DisplayText { get; }

        public bool IsVisible
        {
            get => Registry.GetValue(AppPath, "NoOpenWith", null) == null;
            set
            {
                try
                {
                    if(value) RegistryEx.DeleteValue(AppPath, "NoOpenWith");
                    else Registry.SetValue(AppPath, "NoOpenWith", "");
                }
                catch
                {
                    // 写权限不足时回滚到真实值
                    OnPropertyChanged();
                }
            }
        }

        public bool Delete(out string error)
        {
            error = null;
            try { RegistryEx.DeleteKeyTree(RegPath); return true; }
            catch(Exception ex) { error = ex.Message; return false; }
        }

        private static string GetDisplayName(string appPath, string filePath)
        {
            try
            {
                string name = Registry.GetValue(appPath, "FriendlyAppName", null) as string;
                if(!string.IsNullOrEmpty(name)) name = ResourceString.GetDirectString(name);
                if(string.IsNullOrEmpty(name) && File.Exists(filePath))
                    name = FileVersionInfo.GetVersionInfo(filePath).FileDescription;
                if(string.IsNullOrEmpty(name)) name = Path.GetFileName(filePath);
                return name;
            }
            catch { return Path.GetFileName(filePath); }
        }
    }
}
