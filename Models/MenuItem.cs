#nullable disable
using ContextMenuManager.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace ContextMenuManager.Models
{
    /// <summary>
    /// Shell 类型右键菜单项的数据模型（注册表逻辑，去掉 WinForms/UI）。
    /// 属性直接读写注册表；ViewModel 包装它并向 UI 通知。
    /// </summary>
    public class MenuItem : IMenuItem
    {
        public const string CommandStorePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell";

        private static readonly Dictionary<string, int> DefaultNameIndexs =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "open", 8496 }, { "edit", 8516 }, { "print", 8497 }, { "find", 8503 },
                { "play", 8498 }, { "runas", 8505 }, { "explore", 8502 }, { "preview", 8499 }
            };

        public enum Positions { Default, Top, Bottom }

        public MenuItem(string regPath) => RegPath = regPath;

        public string RegPath { get; set; }

        public string KeyName => RegistryEx.GetKeyName(RegPath);
        public string CommandPath => $@"{RegPath}\command";
        public bool IsOpenItem => KeyName.Equals("open", StringComparison.OrdinalIgnoreCase);
        public bool SupportsVisibilityToggle => true;
        public bool IsMultiItem
        {
            get
            {
                object value = Registry.GetValue(RegPath, "SubCommands", null);
                if(value != null) return true;
                value = Registry.GetValue(RegPath, "ExtendedSubCommandsKey", null);
                return !string.IsNullOrEmpty(value?.ToString());
            }
        }

        public Guid Guid
        {
            get
            {
                var keyValues = new Dictionary<string, string>
                {
                    { CommandPath, "DelegateExecute" },
                    { $@"{RegPath}\DropTarget", "CLSID" },
                    { RegPath, "ExplorerCommandHandler" },
                };
                foreach(var item in keyValues)
                {
                    string value = Registry.GetValue(item.Key, item.Value, null)?.ToString();
                    if(GuidEx.TryParse(value, out Guid guid)) return guid;
                }
                return Guid.Empty;
            }
        }

        public string ItemFilePath => GuidInfo.GetFilePath(Guid) ?? ObjectPath.ExtractFilePath(ItemCommand);

        public string ItemText
        {
            get
            {
                List<string> valueNames = new List<string> { "MUIVerb" };
                if(!IsMultiItem) valueNames.Add("");
                foreach(string valueName in valueNames)
                {
                    string name = Registry.GetValue(RegPath, valueName, null)?.ToString();
                    name = ResourceString.GetDirectString(name);
                    if(!string.IsNullOrEmpty(name)) return name;
                }
                if(DefaultNameIndexs.TryGetValue(KeyName, out int index))
                {
                    string name = $"@windows.storage.dll,-{index}";
                    name = ResourceString.GetDirectString(name);
                    if(!string.IsNullOrEmpty(name)) return name;
                }
                return KeyName;
            }
            set
            {
                if(ResourceString.GetDirectString(value).Length >= 80) return;
                Registry.SetValue(RegPath, "MUIVerb", value);
            }
        }

        public string ItemCommand
        {
            get
            {
                if(IsMultiItem) return null;
                return Registry.GetValue(CommandPath, "", null)?.ToString();
            }
            set => Registry.SetValue(CommandPath, "", value);
        }

        public bool ItemVisible
        {
            get
            {
                try
                {
                    if(WinOsVersion.Current >= WinOsVersion.Win10_1703)
                    {
                        if(SafeToInt(Registry.GetValue(RegPath, "HideBasedOnVelocityId", 0)) == 0x639bc8) return false;
                    }
                    if(!IsMultiItem)
                    {
                        if(Registry.GetValue(RegPath, "LegacyDisable", null) != null) return false;
                        if(Registry.GetValue(RegPath, "ProgrammaticAccessOnly", null) != null) return false;
                        if(WinOsVersion.Current > WinOsVersion.Vista && SafeToInt(Registry.GetValue(RegPath, "CommandFlags", 0)) % 16 >= 8) return false;
                    }
                }
                catch { /* 注册表值类型异常/读取失败时默认视为可见，避免单项异常拖垮整个列表 */ }
                return true;
            }
            set
            {
                void DeleteSomeValues()
                {
                    RegistryEx.DeleteValue(RegPath, "LegacyDisable");
                    RegistryEx.DeleteValue(RegPath, "ProgrammaticAccessOnly");
                    if(WinOsVersion.Current > WinOsVersion.Vista && SafeToInt(Registry.GetValue(RegPath, "CommandFlags", 0)) % 16 >= 8)
                        RegistryEx.DeleteValue(RegPath, "CommandFlags");
                }
                if(value)
                {
                    RegistryEx.DeleteValue(RegPath, "HideBasedOnVelocityId");
                    DeleteSomeValues();
                }
                else
                {
                    if(WinOsVersion.Current >= WinOsVersion.Win10_1703)
                        RegistryEx.SetValue(RegPath, "HideBasedOnVelocityId", 0x639bc8, RegistryValueKind.DWord);
                    else if(!IsMultiItem) return;
                    if(!IsMultiItem)
                    {
                        if(!RegPath.StartsWith(@"HKEY_CLASSES_ROOT\Folder\shell\opennewwindow", StringComparison.OrdinalIgnoreCase))
                        {
                            RegistryEx.SetValue(RegPath, "LegacyDisable", "");
                        }
                        RegistryEx.SetValue(RegPath, "ProgrammaticAccessOnly", "");
                    }
                    if(ShowAsDisabledIfHidden) DeleteSomeValues();
                }
            }
        }

        public string IconLocation
        {
            get => Registry.GetValue(RegPath, "Icon", null)?.ToString();
            set
            {
                if(value != null) Registry.SetValue(RegPath, "Icon", value);
                else RegistryEx.DeleteValue(RegPath, "Icon");
            }
        }

        public bool HasLUAShield
        {
            get => Registry.GetValue(RegPath, "HasLUAShield", null) != null;
            set
            {
                if(value) Registry.SetValue(RegPath, "HasLUAShield", "");
                else RegistryEx.DeleteValue(RegPath, "HasLUAShield");
            }
        }

        public bool OnlyWithShift
        {
            get => Registry.GetValue(RegPath, "Extended", null) != null;
            set
            {
                if(value) Registry.SetValue(RegPath, "Extended", "");
                else RegistryEx.DeleteValue(RegPath, "Extended");
            }
        }

        public bool OnlyInExplorer
        {
            get => Registry.GetValue(RegPath, "OnlyInBrowserWindow", null) != null;
            set
            {
                if(value) Registry.SetValue(RegPath, "OnlyInBrowserWindow", "");
                else RegistryEx.DeleteValue(RegPath, "OnlyInBrowserWindow");
            }
        }

        public bool NoWorkingDirectory
        {
            get => Registry.GetValue(RegPath, "NoWorkingDirectory", null) != null;
            set
            {
                if(value) Registry.SetValue(RegPath, "NoWorkingDirectory", "");
                else RegistryEx.DeleteValue(RegPath, "NoWorkingDirectory");
            }
        }

        public bool NeverDefault
        {
            get => Registry.GetValue(RegPath, "NeverDefault", null) != null;
            set
            {
                if(value) Registry.SetValue(RegPath, "NeverDefault", "");
                else RegistryEx.DeleteValue(RegPath, "NeverDefault");
            }
        }

        public bool ShowAsDisabledIfHidden
        {
            get => Registry.GetValue(RegPath, "ShowAsDisabledIfHidden", null) != null;
            set
            {
                if(value) Registry.SetValue(RegPath, "ShowAsDisabledIfHidden", "");
                else RegistryEx.DeleteValue(RegPath, "ShowAsDisabledIfHidden");
                if(value && !ItemVisible) ItemVisible = false;
            }
        }

        public Positions ItemPosition
        {
            get
            {
                string value = Registry.GetValue(RegPath, "Position", null)?.ToString()?.ToLowerInvariant();
                return value switch
                {
                    "top" => Positions.Top,
                    "bottom" => Positions.Bottom,
                    _ => Positions.Default
                };
            }
            set
            {
                switch(value)
                {
                    case Positions.Top: Registry.SetValue(RegPath, "Position", "top"); break;
                    case Positions.Bottom: Registry.SetValue(RegPath, "Position", "bottom"); break;
                    default: RegistryEx.DeleteValue(RegPath, "Position"); break;
                }
            }
        }

        public void DeleteMe() => RegistryEx.DeleteKeyTree(RegPath, true);

        /// <summary>将注册表值安全转为 int；值为非数字类型(如 REG_BINARY/字符串)或 null 时返回 0。</summary>
        private static int SafeToInt(object value)
        {
            try { return value == null ? 0 : Convert.ToInt32(value); }
            catch { return 0; }
        }
    }
}
