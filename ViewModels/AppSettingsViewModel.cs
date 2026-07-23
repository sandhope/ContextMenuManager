#nullable disable
using ContextMenuManager.Core;

namespace ContextMenuManager.ViewModels
{
    /// <summary>程序设置视图模型，包装 AppConfig 静态属性并提供通知</summary>
    public class AppSettingsViewModel : ObservableObject
    {
        // setter 写入 AppConfig 后触发通知，保证 ToggleSwitch 双向绑定回显与外部变更同步。
        public bool AutoBackup { get => AppConfig.AutoBackup; set { AppConfig.AutoBackup = value; OnPropertyChanged(); } }
        public bool ShowFilePath { get => AppConfig.ShowFilePath; set { AppConfig.ShowFilePath = value; OnPropertyChanged(); } }
        public bool HideDisabledItems { get => AppConfig.HideDisabledItems; set { AppConfig.HideDisabledItems = value; OnPropertyChanged(); } }
        public bool HideSysStoreItems { get => AppConfig.HideSysStoreItems; set { AppConfig.HideSysStoreItems = value; OnPropertyChanged(); } }
        public bool ProtectOpenItem { get => AppConfig.ProtectOpenItem; set { AppConfig.ProtectOpenItem = value; OnPropertyChanged(); } }
        public bool TopMost { get => AppConfig.TopMost; set { AppConfig.TopMost = value; OnPropertyChanged(); } }
        public bool WinXSortable { get => AppConfig.WinXSortable; set { AppConfig.WinXSortable = value; OnPropertyChanged(); } }
        public bool OpenMoreRegedit { get => AppConfig.OpenMoreRegedit; set { AppConfig.OpenMoreRegedit = value; OnPropertyChanged(); } }
        public bool OpenMoreExplorer { get => AppConfig.OpenMoreExplorer; set { AppConfig.OpenMoreExplorer = value; OnPropertyChanged(); } }

        public int UpdateFrequency
        {
            get => AppConfig.UpdateFrequency;
            set { AppConfig.UpdateFrequency = value; OnPropertyChanged(); }
        }
    }
}
