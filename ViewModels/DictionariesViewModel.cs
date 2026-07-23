#nullable disable
using ContextMenuManager.Core;
using ContextMenuManager.Localization;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace ContextMenuManager.ViewModels
{
    /// <summary>字典条目：描述单个字典文件的分类、路径与状态。</summary>
    public class DictionaryEntry : ObservableObject
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Path { get; set; }
        public string Detail { get; set; }
        public string IconGlyph => "\uE8A5"; // 文档图标
    }

    /// <summary>
    /// 字典页视图模型：汇总程序内置字典与运行时（网络/用户）字典目录的文件状态，
    /// 提供打开字典目录与刷新能力，使字典数据来源对用户透明可查。
    /// </summary>
    public class DictionariesViewModel : ObservableObject
    {
        private static readonly string BundledDicsDir =
            System.IO.Path.Combine(AppContext.BaseDirectory, "Assets");

        public ObservableCollection<DictionaryEntry> Items { get; } = new();

        private string _statusText = string.Empty;
        public string StatusText { get => _statusText; set => Set(ref _statusText, value); }

        /// <summary>字典机制说明文本（走 i18n，语言切换后页面重建重新取值）。</summary>
        public string Description => AppStrings.Instance.Get("Other.Dictionaries");

        public void Load()
        {
            Items.Clear();
            try
            {
                AddCategory(AppStrings.Instance.Get("Other.BundledDictionaries"), BundledDicsDir);
                AddCategory(AppStrings.Instance.Get("Other.WebDictionaries"), AppConfig.WebDicsDir);
                AddCategory(AppStrings.Instance.Get("Other.UserDictionaries"), AppConfig.UserDicsDir);
                StatusText = AppStrings.Instance.Get("Status.CountItems", Items.Count);
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.LoadFailed", ex.Message);
            }
        }

        /// <summary>扫描目录下的字典文件（仅 .xml/.ini），逐个加入列表。</summary>
        private void AddCategory(string category, string dir)
        {
            if(string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
            foreach(string path in Directory.EnumerateFiles(dir))
            {
                string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                if(ext != ".xml" && ext != ".ini") continue;
                var info = new FileInfo(path);
                Items.Add(new DictionaryEntry
                {
                    Name = info.Name,
                    Category = category,
                    Path = path,
                    Detail = AppStrings.Instance.Get("Other.DicFileDetail",
                        FormatSize(info.Length), info.LastWriteTime.ToString("yyyy-MM-dd HH:mm"))
                });
            }
        }

        private static string FormatSize(long bytes)
        {
            if(bytes < 1024) return $"{bytes} B";
            double kb = bytes / 1024d;
            return kb < 1024 ? $"{kb:0.#} KB" : $"{kb / 1024d:0.#} MB";
        }

        /// <summary>打开字典根目录（Config\Dictionaries），启动时已确保该目录存在。</summary>
        public void OpenFolder() => ExternalProgram.OpenDirectory(AppConfig.DicsDir);
    }
}
