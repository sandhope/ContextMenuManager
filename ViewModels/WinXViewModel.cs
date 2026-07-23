#nullable disable
using ContextMenuManager.Core;
using ContextMenuManager.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace ContextMenuManager.ViewModels
{
    /// <summary>Win+X 菜单视图模型</summary>
    public class WinXViewModel : ObservableObject
    {
        private static readonly string WinXPath =
            Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Microsoft\Windows\WinX");
        private static readonly string DefaultWinXPath =
            Environment.ExpandEnvironmentVariables(@"%SystemDrive%\Users\Default\AppData\Local\Microsoft\Windows\WinX");

        private WinXGroupNode _selectedGroup;
        private string _statusText = string.Empty;

        public ObservableCollection<WinXGroupNode> Groups { get; } = new();

        public WinXGroupNode SelectedGroup
        {
            get => _selectedGroup;
            set => Set(ref _selectedGroup, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => Set(ref _statusText, value);
        }

        public string FolderPath => WinXPath;

        public void Load()
        {
            try
            {
                Groups.Clear();
                SelectedGroup = null;
                if(!Directory.Exists(WinXPath))
                {
                    StatusText = AppStrings.Instance.Get("Status.WinXFolderMissing", WinXPath);
                    return;
                }
                int total = 0;
                foreach(string dir in Directory.GetDirectories(WinXPath))
                {
                    var group = new WinXGroupNode(dir);
                    foreach(string lnk in Directory.GetFiles(dir, "*.lnk"))
                    {
                        group.Children.Add(new WinXItemNode(lnk));
                        total++;
                    }
                    Groups.Add(group);
                }
                StatusText = AppStrings.Instance.Get("Status.WinXGroupCount", Groups.Count, total);
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.LoadFailed", ex.Message);
            }
        }

        public List<string> GetGroupNames()
        {
            var names = new List<string>();
            if(Directory.Exists(WinXPath))
                foreach(string dir in Directory.GetDirectories(WinXPath))
                    names.Add(System.IO.Path.GetFileName(dir));
            return names;
        }

        /// <summary>添加一个新的 WinX 快捷方式：创建 .lnk（含目标/参数），写入本地化名与 WinX 哈希，使其出现在 Win+X 菜单</summary>
        public bool AddItem(string targetPath, string arguments, string groupName, string itemText)
        {
            try
            {
                if(string.IsNullOrWhiteSpace(targetPath)) { StatusText = AppStrings.Instance.Get("Status.TargetEmpty"); return false; }
                if(!File.Exists(targetPath)) { StatusText = AppStrings.Instance.Get("Status.TargetMissing", targetPath); return false; }
                string dirPath = $@"{WinXPath}\{groupName}";
                if(!Directory.Exists(dirPath)) { StatusText = AppStrings.Instance.Get("Status.GroupMissing", groupName); return false; }

                string fileName = System.IO.Path.GetFileNameWithoutExtension(targetPath);
                int count = Directory.GetFiles(dirPath, "*.lnk").Length;
                string index = (count + 1).ToString().PadLeft(2, '0');
                string lnkName = $"{index} - {fileName}.lnk";
                string lnkPath = ObjectPath.GetNewPathWithIndex($@"{dirPath}\{lnkName}", ObjectPath.PathType.File, 1);
                string display = string.IsNullOrWhiteSpace(itemText) ? fileName : itemText;

                using(var sl = new ShellLink())
                {
                    sl.TargetPath = targetPath;
                    sl.Arguments = arguments ?? string.Empty;
                    sl.WorkingDirectory = System.IO.Path.GetDirectoryName(targetPath);
                    sl.Description = display;
                    sl.Save(lnkPath);
                }
                DesktopIni.SetLocalizedFileNames(lnkPath, display);
                WinXHasher.HashLnk(lnkPath);
                Load();
                StatusText = AppStrings.Instance.Get("Status.AddedNamedColon", display);
                return true;
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.AddFailed", ex.Message);
                return false;
            }
        }

        /// <summary>新建一个 WinX 分组目录（含只读属性与隐藏 desktop.ini）</summary>
        public bool CreateGroup()
        {
            try
            {
                string dirPath = ObjectPath.GetNewPathWithIndex($@"{WinXPath}\Group", ObjectPath.PathType.Directory, 1);
                Directory.CreateDirectory(dirPath);
                string iniPath = $@"{dirPath}\desktop.ini";
                File.WriteAllText(iniPath, string.Empty, Encoding.Unicode);
                File.SetAttributes(dirPath, File.GetAttributes(dirPath) | FileAttributes.ReadOnly);
                File.SetAttributes(iniPath, File.GetAttributes(iniPath) | FileAttributes.Hidden | FileAttributes.System);
                Load();
                StatusText = AppStrings.Instance.Get("Status.GroupCreated", System.IO.Path.GetFileName(dirPath));
                return true;
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.GroupCreateFailed", ex.Message);
                return false;
            }
        }

        public void OpenFolder() => ExternalProgram.OpenDirectory(WinXPath);

        public void DeleteItem(WinXItemNode item)
        {
            try
            {
                if(item == null) return;
                if(File.Exists(item.Path)) File.Delete(item.Path);
                DesktopIni.DeleteLocalizedFileNames(item.Path);
                var group = Groups.FirstOrDefault(g => g.Children.Contains(item));
                group?.Children.Remove(item);
                StatusText = AppStrings.Instance.Get("Status.DeletedNamed", item.Name);
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.DeleteFailed", ex.Message);
            }
        }

        public void RestoreDefault()
        {
            try
            {
                if(!Directory.Exists(DefaultWinXPath))
                {
                    StatusText = AppStrings.Instance.Get("Status.WinXDefaultMissing");
                    return;
                }
                foreach(string dir in Directory.GetDirectories(WinXPath).ToArray())
                {
                    try { Directory.Delete(dir, true); } catch { }
                }
                foreach(string file in Directory.GetFiles(WinXPath).ToArray())
                {
                    try { File.Delete(file); } catch { }
                }
                foreach(string srcDir in Directory.GetDirectories(DefaultWinXPath))
                {
                    CopyDirectory(srcDir, System.IO.Path.Combine(WinXPath, System.IO.Path.GetFileName(srcDir)));
                }
                Load();
                StatusText = AppStrings.Instance.Get("Status.WinXRestored");
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.RestoreFailed", ex.Message);
            }
        }

        private static void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach(string file in Directory.GetFiles(source))
                File.Copy(file, System.IO.Path.Combine(dest, System.IO.Path.GetFileName(file)), true);
            foreach(string dir in Directory.GetDirectories(source))
                CopyDirectory(dir, System.IO.Path.Combine(dest, System.IO.Path.GetFileName(dir)));
        }
    }

    /// <summary>Win+X 分组节点</summary>
    public class WinXGroupNode : ObservableObject
    {
        public WinXGroupNode(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
        }

        public string Path { get; }
        public string Name { get; }
        public ObservableCollection<WinXItemNode> Children { get; } = new();
    }

    /// <summary>Win+X 快捷方式节点</summary>
    public class WinXItemNode : ObservableObject
    {
        public WinXItemNode(string path)
        {
            Path = path;
            Name = GetItemText(path);
        }

        public string Path { get; }

        public string Name { get; }

        private static string GetItemText(string path)
        {
            try
            {
                using(var sl = new ShellLink(path))
                {
                    string d = sl.Description?.Trim();
                    if(!string.IsNullOrEmpty(d)) return d;
                }
            }
            catch { }
            string loc = DesktopIni.GetLocalizedFileNames(path);
            if(!string.IsNullOrEmpty(loc)) return loc;
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }
    }
}
