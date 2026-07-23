#nullable disable
using System;

namespace ContextMenuManager.Models
{
    /// <summary>菜单项统一接口，供 ViewModel 通用绑定</summary>
    public interface IMenuItem
    {
        string RegPath { get; }
        string KeyName { get; }
        string ItemText { get; set; }
        string ItemCommand { get; set; }
        bool ItemVisible { get; set; }
        bool IsOpenItem { get; }
        Guid Guid { get; }
        string ItemFilePath { get; }
        bool SupportsVisibilityToggle { get; }
        void DeleteMe();
    }
}
