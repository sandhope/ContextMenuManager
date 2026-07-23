#nullable disable
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ContextMenuManager.Core
{
    /// <summary>Guid 信息（去掉图像相关，仅保留文本/路径）</summary>
    static class GuidInfo
    {
        public static readonly string[] ClsidPaths =
        {
            @"HKEY_CLASSES_ROOT\CLSID",
            @"HKEY_CLASSES_ROOT\WOW6432Node\CLSID",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Classes\CLSID",
        };

        private static readonly Dictionary<Guid, string> FilePathDic = new Dictionary<Guid, string>();
        private static readonly Dictionary<Guid, string> ClsidPathDic = new Dictionary<Guid, string>();
        private static readonly Dictionary<Guid, string> TextDic = new Dictionary<Guid, string>();

        public static void ReloadDics()
        {
            FilePathDic.Clear();
            ClsidPathDic.Clear();
            TextDic.Clear();
        }

        public static void RemoveDic(Guid guid)
        {
            FilePathDic.Remove(guid);
            ClsidPathDic.Remove(guid);
            TextDic.Remove(guid);
        }

        public static string GetFilePath(Guid guid)
        {
            if(guid.Equals(Guid.Empty)) return null;
            if(FilePathDic.TryGetValue(guid, out string filePath)) return filePath;
            filePath = null;
            foreach(string clsidPath in ClsidPaths)
            {
                using(RegistryKey guidKey = RegistryEx.GetRegistryKey($@"{clsidPath}\{guid:B}"))
                {
                    if(guidKey == null) continue;
                    foreach(string keyName in new[] { "InprocServer32", "LocalServer32" })
                    {
                        using(RegistryKey key = guidKey.OpenSubKey(keyName))
                        {
                            if(key == null) continue;
                            string value1 = key.GetValue("CodeBase")?.ToString()?.Replace("file:///", "")?.Replace('/', '\\');
                            if(File.Exists(value1)) { filePath = value1; break; }
                            string value2 = key.GetValue("")?.ToString();
                            value2 = ObjectPath.ExtractFilePath(value2);
                            if(File.Exists(value2)) { filePath = value2; break; }
                        }
                    }
                    if(File.Exists(filePath))
                    {
                        ClsidPathDic[guid] = guidKey.Name;
                        break;
                    }
                }
            }
            FilePathDic[guid] = filePath;
            return filePath;
        }

        public static string GetClsidPath(Guid guid)
        {
            if(ClsidPathDic.TryGetValue(guid, out string path)) return path;
            foreach(string p in ClsidPaths)
            {
                using(RegistryKey key = RegistryEx.GetRegistryKey($@"{p}\{guid:B}"))
                    if(key != null) return key.Name;
            }
            return null;
        }

        public static string GetText(Guid guid)
        {
            if(guid.Equals(Guid.Empty)) return null;
            if(TextDic.TryGetValue(guid, out string itemText)) return itemText;
            itemText = null;
            foreach(string clsidPath in ClsidPaths)
            {
                foreach(string value in new[] { "LocalizedString", "InfoTip", "" })
                {
                    itemText = Registry.GetValue($@"{clsidPath}\{guid:B}", value, null)?.ToString();
                    itemText = ResourceString.GetDirectString(itemText);
                    if(!itemText.IsNullOrWhiteSpace()) break;
                }
                if(!itemText.IsNullOrWhiteSpace()) break;
            }
            if(itemText.IsNullOrWhiteSpace())
            {
                string filePath = GetFilePath(guid);
                if(File.Exists(filePath))
                {
                    try { itemText = FileVersionInfo.GetVersionInfo(filePath).FileDescription; } catch { }
                    if(itemText.IsNullOrWhiteSpace()) itemText = Path.GetFileName(filePath);
                }
            }
            TextDic[guid] = itemText;
            return itemText;
        }
    }
}
