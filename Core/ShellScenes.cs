#nullable disable
using ContextMenuManager.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ContextMenuManager.Core
{
    /// <summary>右键菜单场景与注册表路径映射</summary>
    public static class ShellScenes
    {
        public const string MENUPATH_FILE = @"HKEY_CLASSES_ROOT\*";
        public const string MENUPATH_FOLDER = @"HKEY_CLASSES_ROOT\Folder";
        public const string MENUPATH_DIRECTORY = @"HKEY_CLASSES_ROOT\Directory";
        public const string MENUPATH_BACKGROUND = @"HKEY_CLASSES_ROOT\Directory\Background";
        public const string MENUPATH_DESKTOP = @"HKEY_CLASSES_ROOT\DesktopBackground";
        public const string MENUPATH_DRIVE = @"HKEY_CLASSES_ROOT\Drive";
        public const string MENUPATH_ALLOBJECTS = @"HKEY_CLASSES_ROOT\AllFilesystemObjects";
        public const string MENUPATH_COMPUTER = @"HKEY_CLASSES_ROOT\CLSID\{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
        public const string MENUPATH_RECYCLEBIN = @"HKEY_CLASSES_ROOT\CLSID\{645FF040-5081-101B-9F08-00AA002F954E}";
        public const string MENUPATH_LIBRARY = @"HKEY_CLASSES_ROOT\LibraryFolder";
        public const string MENUPATH_LIBRARY_BACKGROUND = @"HKEY_CLASSES_ROOT\LibraryFolder\Background";
        public const string MENUPATH_LIBRARY_USER = @"HKEY_CLASSES_ROOT\UserLibraryFolder";
        public const string MENUPATH_UWPLNK = @"HKEY_CLASSES_ROOT\Launcher.ImmersiveApplication";
        public const string MENUPATH_UNKNOWN = @"HKEY_CLASSES_ROOT\Unknown";
        public const string SYSFILEASSPATH = @"HKEY_CLASSES_ROOT\SystemFileAssociations";

        public enum Scenes
        {
            File, Folder, Directory, Background, Desktop, Drive, AllObjects, Computer, RecycleBin, Library,
            LnkFile, UwpLnk, ExeFile, UnknownType, CustomExtension, PerceivedType, DirectoryType,
            CommandStore, DragDrop, CustomRegPath, MenuAnalysis
        }

        /// <summary>跨场景共享的当前选择参数</summary>
        public static class Params
        {
            public static string CurrentExtension = ".txt";
            public static string CurrentPerceivedType = null;
            public static string CurrentDirectoryType = null;
            public static string CurrentCustomRegPath = null;
            public static string CurrentFileObjectPath = null;
        }

        private static string GetShellPath(string scenePath) => $@"{scenePath}\shell";
        private static string GetShellExPath(string scenePath) => $@"{scenePath}\ShellEx";
        private static string GetSysAssExtPath(string typeName) => typeName != null ? $@"{SYSFILEASSPATH}\{typeName}" : null;

        private static string GetOpenMode(string extension)
        {
            string mode = Registry.GetValue($@"{RegistryEx.CLASSES_ROOT}\{extension}", "", null)?.ToString();
            return mode;
        }
        private static string GetOpenModePath(string extension)
        {
            if(extension == null) return null;
            string mode = GetOpenMode(extension);
            // 无关联默认打开方式时返回 null，避免拼出 "HKEY_CLASSES_ROOT\" 无效路径导致下游解析异常。
            return string.IsNullOrEmpty(mode) ? null : $@"{RegistryEx.CLASSES_ROOT}\{mode}";
        }
        private static string GetPerceivedType(string extension)
            => Registry.GetValue($@"{RegistryEx.CLASSES_ROOT}\{extension}", "PerceivedType", null)?.ToString();

        public static List<IMenuItem> Enumerate(Scenes scene)
        {
            var items = new List<IMenuItem>();
            switch(scene)
            {
                case Scenes.File: LoadScene(items, MENUPATH_FILE); break;
                case Scenes.Folder: LoadScene(items, MENUPATH_FOLDER); break;
                case Scenes.Directory: LoadScene(items, MENUPATH_DIRECTORY); break;
                case Scenes.Background: LoadScene(items, MENUPATH_BACKGROUND); break;
                case Scenes.Desktop:
                    if(WinOsVersion.Current != WinOsVersion.Vista) LoadScene(items, MENUPATH_DESKTOP);
                    break;
                case Scenes.Drive: LoadScene(items, MENUPATH_DRIVE); break;
                case Scenes.AllObjects: LoadScene(items, MENUPATH_ALLOBJECTS); break;
                case Scenes.Computer: LoadScene(items, MENUPATH_COMPUTER); break;
                case Scenes.RecycleBin: LoadScene(items, MENUPATH_RECYCLEBIN); break;
                case Scenes.Library:
                    LoadScene(items, MENUPATH_LIBRARY);
                    LoadScene(items, MENUPATH_LIBRARY_BACKGROUND);
                    LoadScene(items, MENUPATH_LIBRARY_USER);
                    break;
                case Scenes.LnkFile:
                    LoadScene(items, GetOpenModePath(".lnk"));
                    break;
                case Scenes.UwpLnk:
                    if(WinOsVersion.Current >= WinOsVersion.Win8) LoadScene(items, MENUPATH_UWPLNK);
                    break;
                case Scenes.ExeFile:
                    LoadScene(items, GetSysAssExtPath(".exe"));
                    LoadScene(items, GetOpenModePath(".exe"));
                    break;
                case Scenes.UnknownType: LoadScene(items, MENUPATH_UNKNOWN); break;
                case Scenes.CustomExtension:
                    {
                        bool isLnk = Params.CurrentExtension?.Equals(".lnk", StringComparison.OrdinalIgnoreCase) == true;
                        if(isLnk) LoadScene(items, GetOpenModePath(".lnk"));
                        else LoadScene(items, GetSysAssExtPath(Params.CurrentExtension));
                        LoadScene(items, GetOpenModePath(Params.CurrentExtension));
                    }
                    break;
                case Scenes.PerceivedType: LoadScene(items, GetSysAssExtPath(Params.CurrentPerceivedType)); break;
                case Scenes.DirectoryType:
                    if(Params.CurrentDirectoryType != null)
                        LoadScene(items, GetSysAssExtPath($"Directory.{Params.CurrentDirectoryType}"));
                    break;
                case Scenes.CommandStore:
                    LoadStoreItems(items);
                    break;
                case Scenes.DragDrop:
                    LoadShellExItems(items, GetShellExPath(MENUPATH_FOLDER));
                    LoadShellExItems(items, GetShellExPath(MENUPATH_DIRECTORY));
                    LoadShellExItems(items, GetShellExPath(MENUPATH_DRIVE));
                    LoadShellExItems(items, GetShellExPath(MENUPATH_ALLOBJECTS));
                    break;
                case Scenes.CustomRegPath: LoadScene(items, Params.CurrentCustomRegPath); break;
                case Scenes.MenuAnalysis:
                    LoadAnalysis(items);
                    break;
            }
            return items;
        }

        private static void LoadScene(List<IMenuItem> items, string scenePath)
        {
            if(scenePath == null) return;
            LoadShellItems(items, GetShellPath(scenePath));
            LoadShellExItems(items, GetShellExPath(scenePath));
        }

        private static void LoadShellItems(List<IMenuItem> items, string shellPath)
        {
            try
            {
                using(RegistryKey shellKey = RegistryEx.GetRegistryKey(shellPath))
                {
                    if(shellKey == null) return;
                    foreach(string keyName in shellKey.GetSubKeyNames())
                        items.Add(new MenuItem($@"{shellPath}\{keyName}"));
                }
            }
            catch { /* 受保护/无权限的注册表子树跳过，保留其余场景的部分结果 */ }
        }

        private static void LoadShellExItems(List<IMenuItem> items, string shellExPath)
        {
            try
            {
                using(RegistryKey shellExKey = RegistryEx.GetRegistryKey(shellExPath))
                {
                    if(shellExKey == null) return;
                    foreach(string subName in shellExKey.GetSubKeyNames())
                    {
                        using(RegistryKey subKey = shellExKey.OpenSubKey(subName))
                        {
                            if(subKey == null) continue;
                            foreach(string handlerName in subKey.GetSubKeyNames())
                            {
                                string handlerPath = $@"{shellExPath}\{subName}\{handlerName}";
                                string value = Registry.GetValue(handlerPath, "", null)?.ToString();
                                if(GuidEx.TryParse(value, out Guid guid))
                                    items.Add(new ShellExItem(guid, handlerPath));
                            }
                        }
                    }
                }
            }
            catch { /* 受保护/无权限的注册表子树跳过，保留其余场景的部分结果 */ }
        }

        /// <summary>返回某场景用于"新建菜单项"的目标 shell 路径（无则 null）</summary>
        public static string GetAddTargetPath(Scenes scene)
        {
            string path = null;
            switch(scene)
            {
                case Scenes.File: path = MENUPATH_FILE; break;
                case Scenes.Folder: path = MENUPATH_FOLDER; break;
                case Scenes.Directory: path = MENUPATH_DIRECTORY; break;
                case Scenes.Background: path = MENUPATH_BACKGROUND; break;
                case Scenes.Desktop: path = WinOsVersion.Current != WinOsVersion.Vista ? MENUPATH_DESKTOP : null; break;
                case Scenes.Drive: path = MENUPATH_DRIVE; break;
                case Scenes.AllObjects: path = MENUPATH_ALLOBJECTS; break;
                case Scenes.Computer: path = MENUPATH_COMPUTER; break;
                case Scenes.RecycleBin: path = MENUPATH_RECYCLEBIN; break;
                case Scenes.Library: path = MENUPATH_LIBRARY; break;
                case Scenes.LnkFile: path = GetOpenModePath(".lnk"); break;
                case Scenes.UwpLnk: path = WinOsVersion.Current >= WinOsVersion.Win8 ? MENUPATH_UWPLNK : null; break;
                case Scenes.ExeFile: path = GetSysAssExtPath(".exe"); break;
                case Scenes.UnknownType: path = MENUPATH_UNKNOWN; break;
                case Scenes.CustomExtension:
                    path = Params.CurrentExtension?.Equals(".lnk", StringComparison.OrdinalIgnoreCase) == true
                        ? GetOpenModePath(".lnk") : GetSysAssExtPath(Params.CurrentExtension);
                    break;
                case Scenes.PerceivedType: path = GetSysAssExtPath(Params.CurrentPerceivedType); break;
                case Scenes.DirectoryType:
                    path = Params.CurrentDirectoryType != null ? GetSysAssExtPath($"Directory.{Params.CurrentDirectoryType}") : null;
                    break;
                case Scenes.CommandStore: path = MenuItem.CommandStorePath; break;
                case Scenes.CustomRegPath: path = Params.CurrentCustomRegPath; break;
            }
            return path == null ? null : GetShellPath(path);
        }

        private static void LoadStoreItems(List<IMenuItem> items)
        {
            using(RegistryKey shellKey = RegistryEx.GetRegistryKey(MenuItem.CommandStorePath))
            {
                if(shellKey == null) return;
                foreach(string itemName in shellKey.GetSubKeyNames())
                {
                    if(AppConfig.HideSysStoreItems && itemName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase)) continue;
                    items.Add(new MenuItem($@"{MenuItem.CommandStorePath}\{itemName}"));
                }
            }
        }

        /// <summary>菜单分析：聚合 Params.CurrentFileObjectPath 所指文件/文件夹实际适用的所有右键菜单来源。</summary>
        private static void LoadAnalysis(List<IMenuItem> items)
        {
            string path = Params.CurrentFileObjectPath;
            if(string.IsNullOrEmpty(path)) return;

            if(Directory.Exists(path))
            {
                LoadScene(items, MENUPATH_DIRECTORY);
                LoadScene(items, MENUPATH_FOLDER);
                LoadScene(items, MENUPATH_ALLOBJECTS);
            }
            else if(File.Exists(path))
            {
                LoadScene(items, MENUPATH_FILE);
                LoadScene(items, MENUPATH_ALLOBJECTS);
                string ext = Path.GetExtension(path);
                if(!string.IsNullOrEmpty(ext))
                {
                    LoadScene(items, GetSysAssExtPath(ext));
                    LoadScene(items, GetOpenModePath(ext));
                    string perceivedType = GetPerceivedType(ext);
                    if(perceivedType != null) LoadScene(items, GetSysAssExtPath(perceivedType));
                }
            }
        }
    }
}
