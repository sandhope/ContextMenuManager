#nullable disable
using ContextMenuManager.Core;
using ContextMenuManager.Localization;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ContextMenuManager.ViewModels
{
    /// <summary>发送到（SendTo）视图模型</summary>
    public class SendToViewModel : ObservableObject
    {
        private static readonly string SendToPath =
            Environment.ExpandEnvironmentVariables(@"%AppData%\Microsoft\Windows\SendTo");
        private static readonly string DefaultSendToPath =
            Environment.ExpandEnvironmentVariables(@"%SystemDrive%\Users\Default\AppData\Roaming\Microsoft\Windows\SendTo");

        private string _statusText = string.Empty;

        public ObservableCollection<SendToItemViewModel> Items { get; } = new();

        public string StatusText
        {
            get => _statusText;
            set => Set(ref _statusText, value);
        }

        public string FolderPath => SendToPath;

        public void Load()
        {
            try
            {
                Items.Clear();
                if(!Directory.Exists(SendToPath))
                {
                    StatusText = AppStrings.Instance.Get("Status.SendToFolderMissing", SendToPath);
                    return;
                }
                foreach(string path in Directory.GetFileSystemEntries(SendToPath))
                {
                    if(Path.GetFileName(path).Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                    Items.Add(new SendToItemViewModel(path));
                }
                StatusText = AppStrings.Instance.Get("Status.CountItems", Items.Count);
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.LoadFailed", ex.Message);
            }
        }

        public void OpenFolder() => ExternalProgram.OpenDirectory(SendToPath);

        public void DeleteItem(SendToItemViewModel item)
        {
            try
            {
                if(item == null) return;
                if(File.Exists(item.Path)) File.Delete(item.Path);
                else if(Directory.Exists(item.Path)) Directory.Delete(item.Path, true);
                Items.Remove(item);
                StatusText = AppStrings.Instance.Get("Status.DeletedNamed", item.DisplayName);
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
                if(!Directory.Exists(DefaultSendToPath))
                {
                    StatusText = AppStrings.Instance.Get("Status.SendToDefaultMissing");
                    return;
                }
                if(Directory.Exists(SendToPath))
                {
                    foreach(string file in Directory.GetFiles(SendToPath).ToArray())
                    {
                        try { File.Delete(file); } catch { }
                    }
                    foreach(string dir in Directory.GetDirectories(SendToPath).ToArray())
                    {
                        try { Directory.Delete(dir, true); } catch { }
                    }
                }
                else
                {
                    Directory.CreateDirectory(SendToPath);
                }
                foreach(string src in Directory.GetFiles(DefaultSendToPath))
                {
                    File.Copy(src, Path.Combine(SendToPath, Path.GetFileName(src)));
                }
                Load();
                StatusText = AppStrings.Instance.Get("Status.SendToRestored");
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.RestoreFailed", ex.Message);
            }
        }
    }

    /// <summary>单个发送到项</summary>
    public class SendToItemViewModel : ObservableObject
    {
        public SendToItemViewModel(string path)
        {
            Path = path;
            DisplayName = System.IO.Path.GetFileNameWithoutExtension(path);
            IsDirectory = Directory.Exists(path);
        }

        public string Path { get; }
        public string DisplayName { get; }
        public bool IsDirectory { get; }

        public string IconGlyph => IsDirectory ? "\uE8B7" : "\uE8A5";
    }
}
