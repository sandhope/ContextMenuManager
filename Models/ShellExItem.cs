#nullable disable
using ContextMenuManager.Core;
using System;

namespace ContextMenuManager.Models
{
    /// <summary>ShellEx（GUID 处理器）类型菜单项，例如 ContextMenuHandlers / DragDropHandlers 下的项</summary>
    public class ShellExItem : IMenuItem
    {
        public ShellExItem(Guid guid, string regPath)
        {
            Guid = guid;
            RegPath = regPath;
        }

        public string RegPath { get; }
        public Guid Guid { get; }
        public string KeyName => RegistryEx.GetKeyName(RegPath);

        public string ItemText
        {
            get
            {
                string text = GuidInfo.GetText(Guid);
                if(!string.IsNullOrEmpty(text)) return text;
                return Guid.ToString("B");
            }
            set { }
        }

        public string ItemCommand { get => null; set { } }
        public string ItemFilePath => GuidInfo.GetFilePath(Guid);

        /// <summary>ShellEx 处理器通常没有简单的隐藏键值，由具体场景决定；默认不提供通用切换</summary>
        public bool ItemVisible { get; set; } = true;
        public bool SupportsVisibilityToggle => false;
        public bool IsOpenItem => false;

        public void DeleteMe() => RegistryEx.DeleteKeyTree(RegPath, true);
    }
}
