#nullable disable
using ContextMenuManager.Core;
using ContextMenuManager.Localization;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ContextMenuManager.ViewModels
{
    /// <summary>增强菜单预设字典项（从 EnhanceMenusDic.xml 解析）</summary>
    public class EnhanceMenuItemDef
    {
        public string SceneRegPath { get; set; }
        public string SceneName { get; set; }
        public string KeyName { get; set; }
        public string Text { get; set; }
        public string Command { get; set; }
        public bool HasLUAShield { get; set; }
        public string Icon { get; set; }
    }

    /// <summary>解析 EnhanceMenusDic.xml，按场景过滤出预设增强项</summary>
    public static class EnhanceMenusDic
    {
        public static List<EnhanceMenuItemDef> Load(string sceneRegPath)
        {
            var result = new List<EnhanceMenuItemDef>();
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "EnhanceMenusDic.xml");
            if(!File.Exists(path)) return result;
            try
            {
                var doc = XDocument.Load(path);
                if(doc.Root == null) return result;
                foreach(var group in doc.Root.Elements("Group"))
                {
                    string regPath = group.Element("RegPath")?.Value.Trim();
                    if(!string.IsNullOrEmpty(sceneRegPath) && !regPath.Equals(sceneRegPath, StringComparison.OrdinalIgnoreCase))
                        continue;
                    string sceneName = group.Elements("Text").FirstOrDefault()?.Attribute("Value")?.Value ?? regPath;
                    var shell = group.Element("Shell");
                    if(shell == null) continue;
                    foreach(var item in shell.Elements("Item"))
                    {
                        string keyName = item.Attribute("KeyName")?.Value;
                        if(string.IsNullOrEmpty(keyName)) continue;

                        string text = item.Elements("Text").FirstOrDefault(t => t.Attribute("Culture") == null)?.Attribute("Value")?.Value
                                   ?? item.Elements("Text").FirstOrDefault()?.Attribute("Value")?.Value
                                   ?? keyName;

                        var regSz = item.Descendants("REG_SZ").FirstOrDefault(r => r.Attribute("Culture") == null)
                                 ?? item.Descendants("REG_SZ").FirstOrDefault();
                        string muiverb = regSz?.Attribute("MUIVerb")?.Value ?? text;
                        bool hasShield = regSz?.Attribute("HasLUAShield") != null;
                        string icon = regSz?.Attribute("Icon")?.Value;

                        string command = null;
                        var cmd = item.Descendants("Command").FirstOrDefault();
                        if(cmd != null)
                        {
                            string fileName = cmd.Attribute("Default")?.Value ?? cmd.Element("FileName")?.Value;
                            string args = cmd.Element("Arguments")?.Value;
                            if(!string.IsNullOrEmpty(fileName))
                                command = string.IsNullOrEmpty(args) ? fileName : $"{fileName} {args}";
                        }

                        result.Add(new EnhanceMenuItemDef
                        {
                            SceneRegPath = regPath,
                            SceneName = sceneName,
                            KeyName = keyName,
                            Text = muiverb,
                            Command = command,
                            HasLUAShield = hasShield,
                            Icon = icon
                        });
                    }
                }
            }
            catch(Exception ex)
            {
                // XML 损坏/解析失败时记录日志并返回已解析部分，避免静默吞没无法定位。
                App.Trace($"EnhanceMenusDic.Load failed: {ex.Message}");
            }
            return result;
        }
    }

    /// <summary>增强菜单页视图模型：按场景列出来自字典的预设增强项，提供一键添加/移除</summary>
    public class EnhanceMenusViewModel : ObservableObject
    {
        public class SceneOption
        {
            public string Name { get; set; }
            public string Path { get; set; }
        }

        public ObservableCollection<SceneOption> Scenes { get; } = new ObservableCollection<SceneOption>
        {
            new SceneOption { Name = AppStrings.Instance.Get("SideBar.File"), Path = @"HKEY_CLASSES_ROOT\*" },
            new SceneOption { Name = AppStrings.Instance.Get("SideBar.Folder"), Path = @"HKEY_CLASSES_ROOT\Directory" },
            new SceneOption { Name = AppStrings.Instance.Get("SideBar.Background"), Path = @"HKEY_CLASSES_ROOT\Directory\Background" },
            new SceneOption { Name = AppStrings.Instance.Get("SideBar.Drive"), Path = @"HKEY_CLASSES_ROOT\Drive" },
            new SceneOption { Name = AppStrings.Instance.Get("SideBar.AllObjects"), Path = @"HKEY_CLASSES_ROOT\AllFilesystemObjects" },
        };

        private SceneOption _selectedScene;
        public SceneOption SelectedScene
        {
            get => _selectedScene;
            set { if(Set(ref _selectedScene, value)) Load(); }
        }

        public ObservableCollection<EnhancePresetItem> Items { get; } = new ObservableCollection<EnhancePresetItem>();

        private string _statusText;
        public string StatusText { get => _statusText; set => Set(ref _statusText, value); }

        public void Load()
        {
            Items.Clear();
            if(SelectedScene == null) return;
            var defs = EnhanceMenusDic.Load(SelectedScene.Path);
            foreach(var def in defs)
                Items.Add(new EnhancePresetItem(def));
            StatusText = AppStrings.Instance.Get("Status.ScenePresetCount", SelectedScene.Name, Items.Count);
        }

        public void Toggle(EnhancePresetItem item)
        {
            if(item == null || SelectedScene == null) return;
            try
            {
                string shellPath = $@"{item.Def.SceneRegPath}\shell\{item.Def.KeyName}";
                if(item.IsPresent)
                {
                    // 移除
                    RegistryEx.DeleteKeyTree(shellPath);
                    item.IsPresent = false;
                    StatusText = AppStrings.Instance.Get("Status.RemovedNamedColon", item.Def.Text);
                }
                else
                {
                    // 添加：写 MUIVerb/Icon/HasLUAShield + command
                    using(RegistryEx.GetRegistryKey(shellPath, true, true)) { }
                    Registry.SetValue(shellPath, "MUIVerb", item.Def.Text);
                    if(!string.IsNullOrEmpty(item.Def.Icon)) Registry.SetValue(shellPath, "Icon", item.Def.Icon);
                    if(item.Def.HasLUAShield) Registry.SetValue(shellPath, "HasLUAShield", "");
                    if(!string.IsNullOrEmpty(item.Def.Command))
                        Registry.SetValue($@"{shellPath}\command", "", item.Def.Command);
                    item.IsPresent = true;
                    StatusText = AppStrings.Instance.Get("Status.AddedNamedColon", item.Def.Text);
                }
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.OperationFailed", ex.Message);
            }
        }
    }

    /// <summary>单个预设增强项（含是否已添加状态）</summary>
    public class EnhancePresetItem : ObservableObject
    {
        private bool _isPresent;

        public EnhanceMenuItemDef Def { get; }

        public EnhancePresetItem(EnhanceMenuItemDef def)
        {
            Def = def;
            try { using var k = RegistryEx.GetRegistryKey($@"{Def.SceneRegPath}\shell\{Def.KeyName}"); _isPresent = k != null; }
            catch { _isPresent = false; }
        }

        public string Text => Def.Text;

        public string Glyph => Def.HasLUAShield ? "\uE72E" : "\uE8A5";

        public bool IsPresent
        {
            get => _isPresent;
            set => Set(ref _isPresent, value);
        }
    }
}
