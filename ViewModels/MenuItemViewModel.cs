#nullable disable
using ContextMenuManager.Core;
using ContextMenuManager.Localization;
using ContextMenuManager.Models;
using System;
using System.Windows.Input;

namespace ContextMenuManager.ViewModels
{
    /// <summary>菜单项视图模型，包装 IMenuItem 并向 UI 提供命令</summary>
    public class MenuItemViewModel : ObservableObject
    {
        private readonly IMenuItem _item;

        public MenuItemViewModel(IMenuItem item) => _item = item;

        public IMenuItem Model => _item;
        public string RegPath => _item.RegPath;
        public string KeyName => _item.KeyName;
        public Guid Guid => _item.Guid;
        public bool IsOpenItem => _item.IsOpenItem;
        public bool SupportsVisibilityToggle => _item.SupportsVisibilityToggle;
        public bool HasGuid => _item.Guid != Guid.Empty;
        public string FilePath => _item.ItemFilePath;

        /// <summary>该项是否指向一个真实存在的文件（用于是否可"打开文件位置"）。</summary>
        public bool HasFilePath => !string.IsNullOrEmpty(FilePath) && System.IO.File.Exists(FilePath);

        public string Text
        {
            get => _item.ItemText;
            set { _item.ItemText = value; OnPropertyChanged(); }
        }

        public string Command
        {
            get => _item.ItemCommand;
            set { _item.ItemCommand = value; OnPropertyChanged(); }
        }

        public bool IsVisible
        {
            get => _item.ItemVisible;
            set
            {
                try
                {
                    _item.ItemVisible = value;
                }
                catch(RegistryAccessException ex)
                {
                    // 写注册表被拒(通常因需要管理员)：回滚 UI(重新读真实值)并提示
                    OnPropertyChanged();
                    RequestElevation?.Invoke(this, ex.RegPath);
                    return;
                }
                OnPropertyChanged();
            }
        }

        public string IconGlyph => "\uE8A5"; // 默认页形图标（WinUI3 内置 Segoe MDL2）

        public event EventHandler RequestDelete;
        public event EventHandler<MenuItemViewModel> RequestEdit;
        public event EventHandler<string> RequestElevation;

        private ICommand _toggleVisibilityCommand;
        public ICommand ToggleVisibilityCommand => _toggleVisibilityCommand ??= new RelayCommand(_ =>
        {
            if(SupportsVisibilityToggle) IsVisible = !IsVisible;
        });

        private ICommand _deleteCommand;
        public ICommand DeleteCommand => _deleteCommand ??= new RelayCommand(_ =>
        {
            try
            {
                _item.DeleteMe();
            }
            catch(RegistryAccessException ex)
            {
                RequestElevation?.Invoke(this, ex.RegPath);
                return;
            }
            RequestDelete?.Invoke(this, EventArgs.Empty);
        });

        private ICommand _editCommand;
        public ICommand EditCommand => _editCommand ??= new RelayCommand(_ => RequestEdit?.Invoke(this, this));

        private ICommand _regLocationCommand;
        public ICommand RegLocationCommand => _regLocationCommand ??= new RelayCommand(_ =>
            ExternalProgram.JumpRegEdit(RegPath, null, AppConfig.OpenMoreRegedit));

        private ICommand _fileLocationCommand;
        public ICommand FileLocationCommand => _fileLocationCommand ??= new RelayCommand(_ =>
        {
            if(!string.IsNullOrEmpty(FilePath) && System.IO.File.Exists(FilePath))
                ExternalProgram.OpenDirectory(System.IO.Path.GetDirectoryName(FilePath));
        });

        private ICommand _copyGuidCommand;
        public ICommand CopyGuidCommand => _copyGuidCommand ??= new RelayCommand(_ =>
        {
            if(!HasGuid) return;
            try
            {
                var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
                package.SetText(Guid.ToString("B"));
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
            }
            catch { }
        });
    }
}
