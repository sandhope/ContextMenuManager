#nullable disable
using ContextMenuManager.Core;
using ContextMenuManager.Localization;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ContextMenuManager.ViewModels
{
    /// <summary>新建菜单（ShellNew）视图模型</summary>
    public class ShellNewViewModel : ObservableObject
    {
        private const string ShellNewPath =
            @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Discardable\PostSetup\ShellNew";

        private string _statusText = string.Empty;

        public ObservableCollection<ShellNewItemViewModel> Items { get; } = new();

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
                var classes = GetClasses();
                foreach(string ext in classes)
                {
                    if(string.IsNullOrEmpty(ext)) continue;
                    Items.Add(new ShellNewItemViewModel(ext));
                }
                StatusText = AppStrings.Instance.Get("Status.CountItemsEnabled", Items.Count);
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.LoadFailed", ex.Message);
            }
        }

        private static string[] GetClasses()
        {
            var value = Registry.GetValue(ShellNewPath, "Classes", null) as string[];
            return value ?? Array.Empty<string>();
        }

        private static void SetClasses(string[] classes)
        {
            Registry.SetValue(ShellNewPath, "Classes", classes, RegistryValueKind.MultiString);
        }

        public void AddItem(string extension)
        {
            try
            {
                extension = NormalizeExtension(extension);
                if(string.IsNullOrEmpty(extension)) { StatusText = AppStrings.Instance.Get("Status.ExtensionEmpty"); return; }

                var classes = GetClasses().ToList();
                if(classes.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                {
                    StatusText = AppStrings.Instance.Get("Status.ItemExists");
                    return;
                }

                // 创建 HKCR\<ext>\ShellNew 子键，默认写入 NullFile
                string snPath = $@"HKEY_CLASSES_ROOT\{extension}\ShellNew";
                using(RegistryKey key = RegistryEx.GetRegistryKey(snPath, true, true))
                {
                    key.SetValue("NullFile", "", RegistryValueKind.String);
                }

                classes.Add(extension);
                SetClasses(classes.ToArray());
                Load();
                StatusText = AppStrings.Instance.Get("Status.AddedNamed", extension);
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.AddFailed", ex.Message);
            }
        }

        public void DeleteItem(ShellNewItemViewModel item)
        {
            try
            {
                if(item == null) return;
                var classes = GetClasses().Where(e => !e.Equals(item.Extension, StringComparison.OrdinalIgnoreCase)).ToArray();
                SetClasses(classes);
                // 同时删除 HKCR\<ext>\ShellNew 子键
                RegistryEx.DeleteKeyTree($@"HKEY_CLASSES_ROOT\{item.Extension}\ShellNew");
                Items.Remove(item);
                StatusText = AppStrings.Instance.Get("Status.DeletedNamed", item.Extension);
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.DeleteFailed", ex.Message);
            }
        }

        /// <summary>开关启用：加入 / 移出 Classes 列表（HKCR 下的 ShellNew 数据保留）</summary>
        public void ToggleItem(ShellNewItemViewModel item, bool enabled)
        {
            try
            {
                if(item == null) return;
                var classes = GetClasses().ToList();
                bool present = classes.Any(e => e.Equals(item.Extension, StringComparison.OrdinalIgnoreCase));
                if(enabled && !present)
                {
                    classes.Add(item.Extension);
                    SetClasses(classes.ToArray());
                    StatusText = AppStrings.Instance.Get("Status.EnabledNamed", item.Extension);
                }
                else if(!enabled && present)
                {
                    SetClasses(classes.Where(e => !e.Equals(item.Extension, StringComparison.OrdinalIgnoreCase)).ToArray());
                    if(Items.Contains(item)) Items.Remove(item);
                    StatusText = AppStrings.Instance.Get("Status.DisabledNamed", item.Extension);
                }
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.OperationFailed", ex.Message);
            }
        }

        public void OpenInRegedit() => ExternalProgram.JumpRegEdit(ShellNewPath, null, false);

        private static string NormalizeExtension(string extension)
        {
            if(string.IsNullOrWhiteSpace(extension)) return string.Empty;
            extension = extension.Trim();
            if(extension.Equals("Folder", StringComparison.OrdinalIgnoreCase)) return "Folder";
            if(!extension.StartsWith(".")) extension = "." + extension;
            return extension.ToLowerInvariant();
        }
    }

    /// <summary>单个新建菜单项</summary>
    public class ShellNewItemViewModel : ObservableObject
    {
        private bool _isEnabled = true;

        public ShellNewItemViewModel(string extension)
        {
            Extension = extension;
            LoadDetails();
        }

        public string Extension { get; }

        public string DetailText { get; private set; } = string.Empty;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => Set(ref _isEnabled, value);
        }

        private void LoadDetails()
        {
            try
            {
                string snPath = $@"HKEY_CLASSES_ROOT\{Extension}\ShellNew";
                using(RegistryKey key = RegistryEx.GetRegistryKey(snPath))
                {
                    if(key == null) { DetailText = AppStrings.Instance.Get("Status.ShellNewNoData"); return; }
                    var parts = new System.Collections.Generic.List<string>();
                    if(key.GetValue("NullFile") != null) parts.Add("NullFile");
                    string fileName = key.GetValue("FileName") as string;
                    if(fileName != null) parts.Add("FileName=" + fileName);
                    string command = key.GetValue("Command") as string;
                    if(command != null) parts.Add("Command");
                    if(key.GetValue("Data") != null) parts.Add("Data");
                    DetailText = parts.Count > 0 ? string.Join("，", parts) : AppStrings.Instance.Get("Status.ShellNewEmpty");
                }
            }
            catch(Exception ex)
            {
                DetailText = AppStrings.Instance.Get("Status.ReadFailed", ex.Message);
            }
        }
    }
}
