#nullable disable
using ContextMenuManager.Core;
using ContextMenuManager.Localization;
using ContextMenuManager.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace ContextMenuManager.ViewModels
{
    /// <summary>Shell 类型菜单列表视图模型（按场景枚举注册表）</summary>
    public class ShellListViewModel : ObservableObject
    {
        private readonly ShellScenes.Scenes _scene;
        private string _statusText;

        public ShellListViewModel(ShellScenes.Scenes scene) => _scene = scene;

        public ObservableCollection<MenuItemViewModel> Items { get; } = new ObservableCollection<MenuItemViewModel>();

        public string StatusText
        {
            get => _statusText;
            set => Set(ref _statusText, value);
        }

        /// <summary>某项需要管理员权限才能修改/删除时触发(由菜单项视图模型转发)</summary>
        public event EventHandler<string> RequestElevation;

        private ICommand _loadCommand;
        public ICommand LoadCommand => _loadCommand ??= new RelayCommand(_ => Load());
        private ICommand _addItemCommand;
        public ICommand AddItemCommand => _addItemCommand ??= new RelayCommand(_ => AddItem());

        public void Load()
        {
            Items.Clear();
            try
            {
                var list = ShellScenes.Enumerate(_scene);
                foreach(var item in list)
                {
                    var vm = new MenuItemViewModel(item);
                    vm.RequestDelete += (s, e) => { if(s is MenuItemViewModel m) Items.Remove(m); };
                    vm.RequestElevation += (s, path) => RequestElevation?.Invoke(s, path);
                    Items.Add(vm);
                }
                StatusText = AppStrings.Instance.Get("Status.CountItems", Items.Count);
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.LoadFailed", ex.Message);
            }
        }

        private void AddItem()
        {
            try
            {
                string shellPath = ShellScenes.GetAddTargetPath(_scene);
                if(shellPath == null) return;
                string newName = ObjectPath.GetNewPathWithIndex($@"{shellPath}\NewItem", ObjectPath.PathType.Registry)
                    .Substring(shellPath.Length + 1);
                string newPath = $@"{shellPath}\{newName}";
                using(Microsoft.Win32.RegistryKey key = RegistryEx.GetRegistryKey(newPath, true, true))
                {
                    if(key == null)
                    {
                        RequestElevation?.Invoke(this, newPath);
                        return;
                    }
                    key.SetValue("", AppStrings.Instance.Get("Other.NewItem"));
                    key.CreateSubKey("command")?.SetValue("", "");
                }
                Load();
            }
            catch(System.Security.SecurityException)
            {
                RequestElevation?.Invoke(this, ShellScenes.GetAddTargetPath(_scene));
            }
            catch(UnauthorizedAccessException)
            {
                RequestElevation?.Invoke(this, ShellScenes.GetAddTargetPath(_scene));
            }
            catch(Exception ex)
            {
                StatusText = AppStrings.Instance.Get("Status.OperationFailed", ex.Message);
            }
        }
    }
}
