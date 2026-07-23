#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ContextMenuManager.Localization
{
    /// <summary>受支持的界面语言，索引与译文数组一一对应；<see cref="ZhCn"/> 为默认与回退语言。</summary>
    public enum AppLang { ZhCn = 0, EnUs = 1 }

    /// <summary>
    /// 代码式多语言服务（参考 SingBoxWin 的 I18nService 设计）：所有翻译以强类型代码内嵌，
    /// 不依赖任何外部语言文件或运行时 INI 解析。以 "Section.Key" 为键，每个键登记各语言译文。
    /// 切换语言仅改变当前语言索引并自增 <see cref="Version"/>，XAML 绑定据此刷新。
    /// </summary>
    public sealed class AppStrings : INotifyPropertyChanged
    {
        private static readonly AppStrings _instance = new AppStrings();
        public static AppStrings Instance => _instance;

        // "Section.Key" -> 各语言译文（下标即 AppLang 值）
        private static readonly Dictionary<string, string[]> _table =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        private AppLang _lang = AppLang.ZhCn;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>切换语言时自增，供 XAML 绑定（TranslateConverter）刷新。</summary>
        private int _version;
        public int Version
        {
            get => _version;
            private set
            {
                _version = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Version)));
            }
        }

        /// <summary>当前生效语言。</summary>
        public AppLang Language => _lang;

        /// <summary>内置语言（id 用于持久化与 XAML，Display 用于列表展示）。</summary>
        public static IReadOnlyList<(string Id, string Display)> BuiltInLanguages { get; } =
            new (string, string)[] { ("zh-CN", "简体中文"), ("en-US", "English") };

        static AppStrings() => Populate();

        /// <summary>切换语言（langId 形如 "zh-CN"/"en-US"，空或未知回退中文），自增 Version 通知刷新。</summary>
        public void LoadLanguage(string langId)
        {
            _lang = string.Equals(langId, "en-US", StringComparison.OrdinalIgnoreCase)
                ? AppLang.EnUs : AppLang.ZhCn;
            Version++;
        }

        /// <summary>取值，键缺失时回退为键名本身；当前语言译文缺失时回退到中文基底。</summary>
        public string Get(string dottedKey)
        {
            if(_table.TryGetValue(dottedKey, out string[] arr))
            {
                int i = (int)_lang;
                string value = i < arr.Length ? arr[i] : null;
                if(string.IsNullOrEmpty(value)) value = arr.Length > 0 ? arr[0] : null;
                return value ?? dottedKey;
            }
            return dottedKey;
        }

        /// <summary>取值并按 {0}/{1} 占位符格式化，键缺失或格式错误时安全回退。</summary>
        public string Get(string dottedKey, params object[] args)
        {
            string value = Get(dottedKey);
            if(args == null || args.Length == 0) return value;
            try { return string.Format(value, args); }
            catch(FormatException) { return value; }
        }

        // 登记一个键的中文/英文译文
        private static void A(string key, string zh, string en) => _table[key] = new[] { zh, en };

        private static void Populate()
        {
            // [ToolBar]
            A("ToolBar.Home", "主页", "Home");
            A("ToolBar.Type", "文件类型", "File Types");
            A("ToolBar.Rule", "其他规则", "Other Rules");
            A("ToolBar.Refresh", "刷新", "Refresh");
            A("ToolBar.About", "关于", "About");

            // [SideBar]
            A("SideBar.File", "文件", "File");
            A("SideBar.Folder", "文件夹", "Folder");
            A("SideBar.Directory", "目录", "Directory");
            A("SideBar.Background", "目录背景", "Directory Background");
            A("SideBar.Desktop", "桌面背景", "Desktop Background");
            A("SideBar.Drive", "磁盘分区", "Drive");
            A("SideBar.AllObjects", "所有对象", "All Objects");
            A("SideBar.Computer", "此电脑", "This PC");
            A("SideBar.RecycleBin", "回收站", "Recycle Bin");
            A("SideBar.Library", "库", "Library");
            A("SideBar.New", "新建菜单", "New Menu");
            A("SideBar.SendTo", "发送到", "Send To");
            A("SideBar.OpenWith", "打开方式", "Open With");
            A("SideBar.WinX", "Win+X", "Win+X");
            A("SideBar.LnkFile", "lnk文件", "Lnk File");
            A("SideBar.UwpLnk", "uwp lnk", "UWP Lnk");
            A("SideBar.ExeFile", "exe文件", "Exe File");
            A("SideBar.CustomExtension", "自选格式", "Custom Extension");
            A("SideBar.PerceivedType", "感知类型", "Perceived Type");
            A("SideBar.DirectoryType", "目录类型", "Directory Type");
            A("SideBar.UnknownType", "未知格式", "Unknown Type");
            A("SideBar.MenuAnalysis", "菜单分析", "Menu Analysis");
            A("SideBar.EnhanceMenu", "增强菜单", "Enhance Menu");
            A("SideBar.DetailedEdit", "详细编辑", "Detailed Edit");
            A("SideBar.DragDrop", "右键拖拽", "Drag & Drop");
            A("SideBar.PublicReferences", "公共引用", "Public References");
            A("SideBar.CustomRegPath", "自选路径", "Custom Registry Path");
            A("SideBar.GuidBlocked", "GUID 锁", "GUID Lock");
            A("SideBar.AppSetting", "程序设置", "Settings");
            A("SideBar.AppLanguage", "程序语言", "Language");
            A("SideBar.CheckUpdate", "检测更新", "Check Update");
            A("SideBar.Dictionaries", "程序字典", "Dictionaries");
            A("SideBar.AboutApp", "关于程序", "About");
            A("SideBar.Donate", "捐赠作者", "Donate");

            // [StatusBar]
            A("StatusBar.File", "所有类型文件的右键菜单", "Context menu for all file types");
            A("StatusBar.Folder", "所有文件夹的右键菜单", "Context menu for all folders");
            A("StatusBar.Directory", "所有文件目录的右键菜单", "Context menu for all file directories");
            A("StatusBar.Background", "所有目录背景和桌面背景的右键菜单", "Context menu for all directory and desktop backgrounds");
            A("StatusBar.Desktop", "桌面背景右键菜单", "Context menu for the desktop background");
            A("StatusBar.Drive", "所有磁盘驱动器的右键菜单", "Context menu for all disk drives");
            A("StatusBar.AllObjects", "所有文件系统对象(包括文件、文件夹)的右键菜单", "Context menu for all filesystem objects (files and folders)");
            A("StatusBar.Computer", "此电脑图标的右键菜单", "Context menu for the This PC icon");
            A("StatusBar.RecycleBin", "回收站图标的右键菜单", "Context menu for the Recycle Bin icon");
            A("StatusBar.Library", "所有库和库目录背景的右键菜单", "Context menu for all libraries and library backgrounds");
            A("StatusBar.New", "所有目录背景和桌面背景的右键 \"新建\" 的菜单项目", "\"New\" submenu items for directory and desktop backgrounds");
            A("StatusBar.SendTo", "所有文件系统对象的右键 \"发送到\" 的菜单项目", "\"Send To\" submenu items for all filesystem objects");
            A("StatusBar.OpenWith", "所有文件右键 \"打开方式\" 的菜单项目", "\"Open With\" menu items for all files");
            A("StatusBar.WinX", "Win8~Win10 \"开始\" 按钮的右键 Win+X 菜单项目", "Win+X menu items of the Start button (Win8~Win10)");
            A("StatusBar.LnkFile", "所有LNK快捷方式的右键菜单", "Context menu for all LNK shortcuts");
            A("StatusBar.UwpLnk", "Win8~Win10 UWP应用快捷方式的右键菜单", "Context menu for UWP app shortcuts (Win8~Win10)");
            A("StatusBar.ExeFile", "所有EXE可执行文件的右键菜单", "Context menu for all EXE executables");
            A("StatusBar.CustomExtension", "自定义指定格式文件的右键菜单", "Context menu for a custom file extension");
            A("StatusBar.PerceivedType", "自定义指定文件感知类型的右键菜单", "Context menu for a custom file perceived type");
            A("StatusBar.DirectoryType", "自定义指定目录感知类型的右键菜单", "Context menu for a custom directory type");
            A("StatusBar.UnknownType", "所有未关联打开方式的格式文件的右键菜单", "Context menu for files without an associated open method");
            A("StatusBar.MenuAnalysis", "分析指定文件对象所有的右键菜单所在位置", "Analyze where all context menus of a given object live");
            A("StatusBar.EnhanceMenu", "添加一些强大便捷的菜单项", "Add some powerful and handy menu items");
            A("StatusBar.DetailedEdit", "系统和其他程序内部部分菜单详细设置规则", "Detailed rules for some system and third-party menus");
            A("StatusBar.DragDrop", "右键拖拽文件时的菜单项目", "Menu items shown when dragging files with the right button");
            A("StatusBar.PublicReferences", "编辑用户添加的公共引用的Shell类型子菜单项目", "Edit user-added public reference shell submenu items");
            A("StatusBar.CustomRegPath", "编辑自定义注册表路径的右键菜单项目", "Edit context menu items at a custom registry path");
            A("StatusBar.GuidBlocked", "适用于隐藏部分顽固的依赖GUID的ShellEx类型项目", "Hide stubborn GUID-dependent ShellEx items");

            // [Menu]
            A("Menu.ChangeText", "更改文本", "Change Text");
            A("Menu.ItemIcon", "菜单图标", "Menu Icon");
            A("Menu.ChangeIcon", "更改图标", "Change Icon");
            A("Menu.ShieldIcon", "盾牌图标", "Shield Icon");
            A("Menu.AddIcon", "添加图标", "Add Icon");
            A("Menu.DeleteIcon", "删除图标", "Remove Icon");
            A("Menu.ItemPosition", "菜单位置", "Menu Position");
            A("Menu.SetDefault", "默认", "Default");
            A("Menu.SetTop", "置顶", "Top");
            A("Menu.SetBottom", "置底", "Bottom");
            A("Menu.OtherAttributes", "其他属性", "Other Attributes");
            A("Menu.OnlyWithShift", "仅在按住 Shift 键后显示", "Show only when Shift is held");
            A("Menu.OnlyInExplorer", "仅在 Explorer 窗口显示", "Show only in Explorer windows");
            A("Menu.NoWorkingDirectory", "不使用右键所处目录信息", "Do not use the current directory info");
            A("Menu.NeverDefault", "从不用作左键默认执行命令", "Never used as the default left-click command");
            A("Menu.ShowAsDisabledIfHidden", "被禁用时灰色显示不隐藏", "Show greyed out instead of hidden when disabled");
            A("Menu.Details", "详细信息", "Details");
            A("Menu.WebSearch", "网页搜索", "Web Search");
            A("Menu.ChangeCommand", "更改命令", "Change Command");
            A("Menu.RunAsAdministrator", "提升权限", "Elevate Privileges");
            A("Menu.FileProperties", "文件属性", "File Properties");
            A("Menu.FileLocation", "文件位置", "File Location");
            A("Menu.Save", "保存", "Save");
            A("Menu.Cancel", "取消", "Cancel");
            A("Menu.Edit", "编辑", "Edit");
            A("Menu.RegistryLocation", "注册表位置", "Registry Location");
            A("Menu.ExportRegistry", "导出注册表", "Export Registry");
            A("Menu.Delete", "删除此项", "Delete Item");
            A("Menu.DeleteReference", "删除引用", "Delete Reference");
            A("Menu.HandleGuid", "处理GUID", "Handle GUID");
            A("Menu.CopyGuid", "复制GUID", "Copy GUID");
            A("Menu.BlockGuid", "锁定GUID", "Lock GUID");
            A("Menu.ClsidLocation", "CLSID路径", "CLSID Path");
            A("Menu.AddGuidDic", "添加字典", "Add Dictionary");
            A("Menu.InitialData", "编辑文件初始数据", "Edit File Initial Data");
            A("Menu.BeforeSeparator", "显示在分割线上方", "Show above the separator");
            A("Menu.ChangeGroup", "更换分组", "Change Group");
            A("Menu.RestoreDefault", "还原默认", "Restore Default");
            A("Menu.FoldAll", "全部折叠", "Collapse All");
            A("Menu.UnfoldAll", "全部展开", "Expand All");

            // [Dialog]
            A("Dialog.Browse", "浏览", "Browse");
            A("Dialog.Program", "程序", "Program");
            A("Dialog.AllFiles", "所有文件", "All Files");
            A("Dialog.RegistryFile", "注册表文件", "Registry File");
            A("Dialog.ItemText", "菜单文本", "Menu Text");
            A("Dialog.ItemCommand", "菜单命令", "Menu Command");
            A("Dialog.CommandArguments", "命令参数", "Command Arguments");
            A("Dialog.SingleMenu", "一级", "Single");
            A("Dialog.MultiMenu", "多级", "Multi-level");
            A("Dialog.Public", "公有", "Public");
            A("Dialog.Private", "私有", "Private");
            A("Dialog.SelectAll", "全选", "Select All");
            A("Dialog.InputGuid", "输入Guid", "Enter GUID");
            A("Dialog.AddGuidDic", "添加Guid本地字典", "Add local GUID dictionary");
            A("Dialog.DeleteGuidDic", "删除", "Delete");
            A("Dialog.NoPerceivedType", "无感知", "None");
            A("Dialog.TextFile", "文本文件", "Text File");
            A("Dialog.DocumentFile", "文档文件", "Document File");
            A("Dialog.ImageFile", "图像文件", "Image File");
            A("Dialog.VideoFile", "视频文件", "Video File");
            A("Dialog.AudioFile", "音频文件", "Audio File");
            A("Dialog.NeedAdmin", "需要管理员权限", "Administrator privileges required");
            A("Dialog.NeedAdminTip", "修改或删除此注册表项需要管理员权限。", "Modifying or deleting this registry key requires administrator privileges.");
            A("Dialog.RunAsAdmin", "以管理员身份运行", "Run as administrator");
            A("Dialog.SystemProtectedTip", "此注册表项由系统(TrustedInstaller)拥有，已尝试取得其所有权但仍无法修改，可能被安全软件保护或受系统策略限制。", "This registry key is owned by the system (TrustedInstaller). Ownership takeover was attempted but modification still failed; it may be protected by security software or restricted by system policy.");
            A("Dialog.ElevationPending", "请先关闭当前对话框，再处理权限请求。", "Please close the current dialog before handling the elevation request.");
            A("Dialog.WinXAddTitle", "添加 WinX 项", "Add WinX Item");
            A("Dialog.WinXTargetPath", "目标程序路径", "Target program path");
            A("Dialog.WinXArguments", "参数（可选）", "Arguments (optional)");
            A("Dialog.WinXGroup", "分组", "Group");
            A("Dialog.WinXDisplayText", "显示文本（可选，留空则用程序名）", "Display text (optional, program name if empty)");
            A("Dialog.NoGroupTip", "没有可用分组，请先新建分组", "No group available, please create a group first");
            A("Dialog.ShellNewAddTitle", "添加新建菜单项", "Add New Menu Item");
            A("Dialog.ShellNewExtension", "扩展名（如 txt、bmp，文件夹为 Folder）", "Extension (e.g. txt, bmp; use Folder for folders)");
            A("Dialog.CompressedFile", "压缩文件", "Compressed File");
            A("Dialog.SystemFile", "系统文件", "System File");
            A("Dialog.DocumentDirectory", "文档目录", "Document Directory");
            A("Dialog.ImageDirectory", "图像目录", "Image Directory");
            A("Dialog.VideoDirectory", "视频目录", "Video Directory");
            A("Dialog.AudioDirectory", "音频目录", "Audio Directory");
            A("Dialog.EditSubItems", "编辑 \"%s\" 的子菜单项目", "Edit submenu items of \"%s\"");
            A("Dialog.DetailedEdit", "详细编辑 \"%s\" 菜单项目", "Detailed edit of the \"%s\" menu item");
            A("Dialog.CheckReference", "请勾选你想要引用的菜单项目", "Please check the menu items you want to reference");
            A("Dialog.CheckCopy", "请勾选你想要复制的菜单项目", "Please check the menu items you want to copy");
            A("Dialog.SelectExtension", "请选择一个文件扩展名", "Please select a file extension");
            A("Dialog.SelectPerceivedType", "请选择一个文件感知类型", "Please select a file perceived type");
            A("Dialog.SelectDirectoryType", "请选择一个目录感知类型", "Please select a directory type");
            A("Dialog.SelectGroup", "请选择保存分组", "Please select a group to save");
            A("Dialog.SelectNewItemType", "请选择新建菜单类型", "Please select a new menu type");
            A("Dialog.SelectObjectType", "请选择要分析的对象类型", "Please select the object type to analyze");
            A("Dialog.SelectDropEffect", "请选择拖拽文件默认命令", "Please select the default drag command");
            A("Dialog.DefaultDropEffect", "默认(同盘移动、异盘复制)", "Default (move on same drive, copy across drives)");
            A("Dialog.CopyDropEffect", "复制(Ctrl)", "Copy (Ctrl)");
            A("Dialog.MoveDropEffect", "移动(Shift)", "Move (Shift)");
            A("Dialog.CreateLinkDropEffect", "创建快捷方式(Alt)", "Create shortcut (Alt)");
            A("Dialog.DownloadLanguages", "下载语言文件", "Download language files");
            A("Dialog.TranslateTool", "翻译工具", "Translation Tool");
            A("Dialog.DefaultText", "默认文本", "Default Text");
            A("Dialog.OldTranslation", "旧译文", "Old Translation");
            A("Dialog.NewTranslation", "新译文", "New Translation");
            A("Dialog.DonateInfo", "此名单不定期更新", "This list is updated occasionally");

            // [Page]
            A("Page.AppTitle", "Windows 右键管理", "Windows Context Menu Manager");
            A("Page.Search", "搜索菜单…", "Search menus…");
            A("Page.New", "新建", "New");
            A("Page.Add", "添加", "Add");
            A("Page.AddItem", "添加项", "Add Item");
            A("Page.NewGroup", "新建分组", "New Group");
            A("Page.OpenFolder", "打开文件夹", "Open Folder");
            A("Page.OpenRegistry", "打开注册表", "Open Registry");
            A("Page.RestoreDefault", "还原默认", "Restore Default");
            A("Page.ApplyLock", "应用锁定", "Apply Lock");
            A("Page.Delete", "删除", "Delete");
            A("Page.TargetScene", "目标场景", "Target Scene");
            A("Page.EnhanceTip", "下列为常用增强菜单预设，开关即添加/移除到对应场景右键菜单", "Below are common enhance-menu presets; toggling adds/removes them from the corresponding scene menu");
            A("Page.InputGuid", "输入 GUID", "Enter GUID");
            A("Page.GuidBlockedTip", "说明：锁定的 GUID 对应的 ShellEx 处理器将被隐藏（备份至 __BlockedBackup 子项，便于还原）。", "Note: ShellEx handlers matching the locked GUIDs will be hidden (backed up under __BlockedBackup for restore).");
            A("Page.SelectLanguage", "选择程序语言", "Select language");
            A("Page.GithubRepo", "Github 仓库", "Github Repo");

            // [Input]
            A("Input.Apply", "应用", "Apply");
            A("Input.BrowseFile", "选择文件", "Choose File");
            A("Input.BrowseFolder", "选择文件夹", "Choose Folder");
            A("Input.ExtensionLabel", "文件扩展名", "File Extension");
            A("Input.ExtensionPlaceholder", "例如：.txt", "e.g. .txt");
            A("Input.PerceivedTypeLabel", "感知类型", "Perceived Type");
            A("Input.PerceivedTypePlaceholder", "例如：text、image、audio、video、compressed、system", "e.g. text, image, audio, video, compressed, system");
            A("Input.DirectoryTypeLabel", "目录类型", "Directory Type");
            A("Input.DirectoryTypePlaceholder", "例如：Music、Pictures、Video", "e.g. Music, Pictures, Video");
            A("Input.CustomRegPathLabel", "自定义注册表路径", "Custom Registry Path");
            A("Input.CustomRegPathPlaceholder", "例如：HKEY_CLASSES_ROOT\\*\\shell", "e.g. HKEY_CLASSES_ROOT\\*\\shell");
            A("Input.AnalysisLabel", "菜单分析", "Menu Analysis");
            A("Input.AnalysisPlaceholder", "点击“选择文件”或“选择文件夹”进行分析", "Click \"Choose File\" or \"Choose Folder\" to analyze");

            // [Setting]
            A("Setting.AutoBackup", "删除菜单时自动备份注册表", "Auto-backup registry when deleting menu items");
            A("Setting.ShowFilePath", "状态栏实时显示文件路径", "Show file path in status bar in real time");
            A("Setting.HideDisabledItems", "隐藏已禁用的菜单项目", "Hide disabled menu items");
            A("Setting.HideSysStoreItems", "隐藏公共引用中的系统菜单", "Hide system menus in public references");
            A("Setting.ProtectOpenItem", "保护名称为“打开”的菜单项目", "Protect menu items named \"Open\"");
            A("Setting.TopMost", "使窗口始终在屏幕最上方", "Keep window always on top");
            A("Setting.WinXSortable", "启用 WinX 菜单排序功能", "Enable WinX menu sorting");
            A("Setting.OpenMoreRegedit", "注册表编辑器窗口多开", "Allow multiple Registry Editor windows");
            A("Setting.OpenMoreExplorer", "文件资源管理器窗口多开", "Allow multiple File Explorer windows");
            A("Setting.UpdateFrequency", "设置程序自动检查更新频率", "Set the automatic update check frequency");

            // [Status]
            A("Status.CountItems", "共 {0} 项", "{0} item(s) in total");
            A("Status.EmptyList", "当前场景暂无可管理的菜单项目。", "There are no menu items to manage in this scene yet.");
            A("Status.CountItemsEnabled", "共 {0} 项（已启用）", "{0} item(s) in total (enabled)");
            A("Status.CountPrograms", "共 {0} 个程序", "{0} program(s) in total");
            A("Status.CountRules", "共 {0} 条规则", "{0} rule(s) in total");
            A("Status.CountLockedGuid", "共锁定 {0} 个 GUID", "{0} GUID(s) locked in total");
            A("Status.WinXGroupCount", "共 {0} 个分组，{1} 个快捷方式", "{0} group(s), {1} shortcut(s)");
            A("Status.ScenePresetCount", "场景：{0}，共 {1} 项预设", "Scene: {0}, {1} preset(s) in total");
            A("Status.LoadFailed", "加载失败：{0}", "Load failed: {0}");
            A("Status.ReadFailed", "读取失败：{0}", "Read failed: {0}");
            A("Status.AddFailed", "添加失败：{0}", "Add failed: {0}");
            A("Status.DeleteFailed", "删除失败：{0}", "Delete failed: {0}");
            A("Status.SaveFailed", "保存失败：{0}", "Save failed: {0}");
            A("Status.OperationFailed", "操作失败：{0}", "Operation failed: {0}");
            A("Status.RestoreFailed", "还原失败：{0}", "Restore failed: {0}");
            A("Status.ApplyFailed", "应用失败：{0}", "Apply failed: {0}");
            A("Status.GroupCreateFailed", "新建分组失败：{0}", "Failed to create group: {0}");
            A("Status.OpenRegistryFailed", "打开注册表失败：{0}", "Failed to open registry: {0}");
            A("Status.Added", "已添加", "Added");
            A("Status.Deleted", "已删除", "Deleted");
            A("Status.Saved", "已保存", "Saved");
            A("Status.Removed", "已移除", "Removed");
            A("Status.AddedNamedColon", "已添加：{0}", "Added: {0}");
            A("Status.RemovedNamedColon", "已移除：{0}", "Removed: {0}");
            A("Status.AddedNamed", "已添加 {0}", "Added {0}");
            A("Status.DeletedNamed", "已删除 {0}", "Deleted {0}");
            A("Status.EnabledNamed", "已启用 {0}", "Enabled {0}");
            A("Status.DisabledNamed", "已禁用 {0}", "Disabled {0}");
            A("Status.EnabledColon", "已启用：{0}", "Enabled: {0}");
            A("Status.DisabledColon", "已禁用：{0}", "Disabled: {0}");
            A("Status.MenuTextEmpty", "菜单文本不能为空", "Menu text cannot be empty");
            A("Status.ScriptPathEmpty", "脚本路径不能为空", "Script path cannot be empty");
            A("Status.TargetEmpty", "目标程序不能为空", "Target program cannot be empty");
            A("Status.TargetMissing", "目标程序不存在：{0}", "Target program does not exist: {0}");
            A("Status.GroupMissing", "分组不存在：{0}", "Group does not exist: {0}");
            A("Status.GroupCreated", "已新建分组：{0}", "Group created: {0}");
            A("Status.ExtensionEmpty", "扩展名不能为空", "Extension cannot be empty");
            A("Status.ItemExists", "该项已存在", "This item already exists");
            A("Status.InputGuidPrompt", "请输入 GUID", "Please enter a GUID");
            A("Status.GuidInvalid", "GUID 格式不正确", "Invalid GUID format");
            A("Status.GuidAlreadyLocked", "该 GUID 已在锁定列表中", "This GUID is already in the lock list");
            A("Status.LockAdded", "已添加锁定：{0}", "Lock added: {0}");
            A("Status.HiddenCount", "已隐藏 {0} 个匹配项（已备份至 __BlockedBackup）", "{0} matching item(s) hidden (backed up under __BlockedBackup)");
            A("Status.NoMatchShellEx", "未找到匹配的 ShellEx 项", "No matching ShellEx item found");
            A("Status.NotFoundApplications", "未找到 HKCR\\Applications", "HKCR\\Applications not found");
            A("Status.SendToFolderMissing", "SendTo 文件夹不存在：{0}", "SendTo folder does not exist: {0}");
            A("Status.WinXFolderMissing", "WinX 文件夹不存在：{0}", "WinX folder does not exist: {0}");
            A("Status.WinXDefaultMissing", "默认 WinX 文件夹不存在，无法还原", "Default WinX folder does not exist, cannot restore");
            A("Status.WinXRestored", "已还原默认 WinX 菜单", "Default WinX menu restored");
            A("Status.SendToDefaultMissing", "默认 SendTo 文件夹不存在，无法还原", "Default SendTo folder does not exist, cannot restore");
            A("Status.SendToRestored", "已还原默认 SendTo 菜单", "Default SendTo menu restored");
            A("Status.ShellNewNoData", "（无 ShellNew 数据）", "(No ShellNew data)");
            A("Status.ShellNewEmpty", "（空）", "(Empty)");
            A("Status.Unknown", "(未知)", "(Unknown)");

            // [Detailed]
            A("Detailed.ShareName", "共享", "Share");
            A("Detailed.ShareTip", "控制文件右键菜单中的\"共享\"项（经典共享面板）", "Controls the \"Share\" item in the file context menu (classic sharing panel)");
            A("Detailed.ModernShareName", "现代共享", "Modern Share");
            A("Detailed.ModernShareTip", "控制文件右键菜单中的 Win10/11 共享面板项", "Controls the Win10/11 share-panel item in the file context menu");
            A("Detailed.SendToName", "发送到", "Send To");
            A("Detailed.SendToTip", "控制所有文件系统对象右键菜单中的\"发送到\"子菜单", "Controls the \"Send To\" submenu for all filesystem objects");
            A("Detailed.PrevVersionsName", "还原以前的版本", "Restore Previous Versions");
            A("Detailed.PrevVersionsTip", "控制文件/文件夹右键菜单中的\"还原以前的版本\"项", "Controls the \"Restore previous versions\" item for files/folders");
            A("Detailed.DirShareName", "目录共享", "Directory Share");
            A("Detailed.DirShareTip", "控制目录右键菜单中的\"共享\"项", "Controls the \"Share\" item in the directory context menu");
            A("Detailed.LibraryLocationName", "库位置", "Library Location");
            A("Detailed.LibraryLocationTip", "控制文件夹右键菜单中的\"库位置\"项", "Controls the \"Library Location\" item in the folder context menu");
            A("Detailed.PinToStartName", "固定到开始屏幕", "Pin to Start");
            A("Detailed.PinToStartTip", "控制文件夹右键菜单中的\"固定到开始屏幕\"项", "Controls the \"Pin to Start\" item in the folder context menu");
            A("Detailed.PrintDirName", "打印目录", "Print Directory");
            A("Detailed.PrintDirTip", "控制目录右键菜单中的\"打印\"项", "Controls the \"Print\" item in the directory context menu");

            // [Message]
            A("Message.TextCannotBeEmpty", "菜单文本不能为空！", "Menu text cannot be empty!");
            A("Message.CommandCannotBeEmpty", "菜单命令不能为空！", "Menu command cannot be empty!");
            A("Message.StringParsingFailed", "本地化字符串解析失败！", "Failed to parse the localized string!");
            A("Message.TextLengthCannotExceed80", "菜单文本过长，长度不允许超过80！", "Menu text is too long; length must not exceed 80!");
            A("Message.ConfirmDeletePermanently", "确认是否永久删除此项？\n此操作无法还原，请谨慎操作！", "Permanently delete this item?\nThis cannot be undone, please proceed with caution!");
            A("Message.ConfirmDelete", "确认是否删除该项？", "Delete this item?");
            A("Message.ConfirmDeleteReferenced", "确认是否删除此项？\n所有引用此项的项目都会失效，请谨慎操作！", "Delete this item?\nAll items referencing it will break, please proceed with caution!");
            A("Message.CannotAddNewItem", "系统限制子菜单数目最多为16，无法添加更多的子菜单项目！", "The system limits submenus to 16; cannot add more submenu items!");
            A("Message.VistaUnsupportedMulti", "Vista系统不支持多级菜单！", "Vista does not support multi-level menus!");
            A("Message.CannotHideSubItem", "你的系统版本太低，不支持隐藏子级菜单！", "Your Windows version is too old to hide submenus!");
            A("Message.UnsupportedFilename", "不支持的文件名，可能已经存在相同文件名的菜单项目！", "Unsupported file name; a menu item with the same name may already exist!");
            A("Message.NoOpenModeExtension", "此扩展名没有关联默认打开方式，请先为此类型文件关联打开方式！", "This extension has no default open method; please associate one first!");
            A("Message.CannotChangePath", "不允许更改文件路径！", "Changing the file path is not allowed!");
            A("Message.CopiedToClipboard", "已复制到剪切板:", "Copied to clipboard:");
            A("Message.MalformedGuid", "格式不正确的Guid", "Malformed GUID");
            A("Message.HasBeenAdded", "此项目已被添加！", "This item has already been added!");
            A("Message.SelectSubMenuMode", "该多级菜单子项目数为0", "This multi-level menu has 0 sub-items");
            A("Message.PromptIsOpenItem", "该项为文件或文件夹的\"打开\"菜单，盲目操作可能会导致无法打开文件或文件夹，请确认是否继续你的操作？(不建议)", "This is the \"Open\" menu for a file or folder. Blind changes may prevent it from opening. Continue? (Not recommended)");
            A("Message.SelectRegPath", "操作步骤：打开注册表编辑器，导航到目标注册表路径，关闭注册表编辑器窗口。请确认是否继续？", "Steps: open Registry Editor, navigate to the target path, then close Registry Editor. Continue?");
            A("Message.RestartApp", "程序将会重新启动！", "The application will restart!");
            A("Message.FileNotExists", "文件不存在！", "File does not exist!");
            A("Message.FolderNotExists", "文件夹不存在！", "Folder does not exist!");
            A("Message.VersionIsLatest", "当前版本为最新版！", "You are on the latest version!");
            A("Message.WebDataReadFailed", "网络数据读取失败!", "Failed to read network data!");
            A("Message.OpenWebUrl", "是否打开相关网页?", "Open the related web page?");
            A("Message.AuthorityProtection", "此菜单注册表项目可能受安全软件保护，无法对其进行禁用删除和其他个性化修改。", "This menu registry item may be protected by security software and cannot be disabled, deleted or customized.");
            A("Message.WinXSorted", "为优化排序功能已对部分项目重新编号，需要重启文件资源管理器应用效果", "Some items were renumbered to optimize sorting; restart File Explorer to apply");
            A("Message.RestoreDefault", "确认还原为默认菜单项目？", "Restore the default menu items?");
            A("Message.DeleteGroup", "确认永久删除此组及组内所有菜单项目？", "Permanently delete this group and all menu items in it?");

            // [Tip]
            A("Tip.RestartExplorer", "重启Explorer会使桌面闪烁片刻，正常现象无需担心", "Restarting Explorer will briefly flash the desktop; this is normal");
            A("Tip.CustomFolder", "禁用此项将会同时禁用文件系统对象属性面板中的自定义选项卡", "Disabling this also disables the custom tab in the filesystem object properties panel");
            A("Tip.SendToDrive", "仅当插入可移动磁盘时有作用，显示该可移动磁盘的所有分区", "Only works when a removable disk is inserted, showing all its partitions");
            A("Tip.BuildSendtoMenu", "禁用此项将加快主菜单弹出速度但会延缓发送到子菜单弹出速度", "Disabling this speeds up the main menu but slows the Send To submenu");
            A("Tip.InvalidItem", "无效菜单项目将导致此项以下的所有菜单项目不可见(建议删除)", "An invalid menu item hides all items below it (deletion recommended)");
            A("Tip.EditSubItems", "编辑子菜单项目", "Edit submenu items");
            A("Tip.AddReference", "从公共引用项目中添加引用", "Add a reference from public reference items");
            A("Tip.AddFromPublic", "从公共引用中复制菜单项目", "Copy a menu item from public references");
            A("Tip.AddFromParentMenu", "从母菜单中复制项目", "Copy an item from the parent menu");
            A("Tip.AddSeparator", "添加分隔线", "Add a separator");
            A("Tip.DeleteGuidDic", "删除用户自行添加的该项的本地Guid字典", "Delete the user-added local GUID dictionary entry");
            A("Tip.LockNewMenu", "启用后可阻止第三方程序增加项目且可对现有项目排序(关闭后复原)", "When enabled, blocks third-party programs from adding items and allows sorting existing items (restored when disabled)");
            A("Tip.DropOrSelectObject", "请拖拽或通过按钮选择文件或目录", "Please drag, or use the button to select, a file or directory");
            A("Tip.ConfigPath", "更改配置和数据文件保存路径后，会导致部分已启用增强菜单失效，可在增强菜单中重新启用一遍", "Changing the config/data save path disables some enabled enhance menus; re-enable them in Enhance Menu");
            A("Tip.CommandFiles", "此命令依赖配置文件，移动配置文件位置会导致此菜单项失效，重新启用一遍即可", "This command depends on a config file; moving the config location disables it, just re-enable it");
            A("Tip.CreateGroup", "新建一个分组", "Create a new group");
            A("Tip.ImmediatelyCheck", "立即检查", "Check now");

            // [Other]
            A("Other.CustomFolder", "自定义文件夹(&F)...", "Custom Folder(&F)...");
            A("Other.BuildSendtoMenu", "快速构建发送到子菜单", "Quickly build the Send To submenu");
            A("Other.NewItem", "新建一个菜单项目", "Create a new menu item");
            A("Other.AddGuidBlockedItem", "添加GUID锁定项目", "Add a GUID lock item");
            A("Other.LockNewMenu", "锁定新建菜单并启用排序功能", "Lock the New menu and enable sorting");
            A("Other.InvalidItem", "无效菜单项目:", "Invalid menu item:");
            A("Other.Separator", ">>>>>> 分割线 <<<<<<", ">>>>>> Separator <<<<<<");
            A("Other.SelectRegPath", "请选择注册表项", "Please select a registry key");
            A("Other.CurrentExtension", "你当前选择的文件扩展名为 %s", "The currently selected file extension is %s");
            A("Other.CurrentPerceivedType", "你当前选择的文件感知类型为 %s", "The currently selected file perceived type is %s");
            A("Other.CurrentDirectoryType", "你当前选择的目录感知类型为 %s", "The currently selected directory type is %s");
            A("Other.CurrentFilePath", "你当前选择的文件对象路径为", "The currently selected file object path is");
            A("Other.CurrentRegPath", "你当前选择的注册表路径为", "The currently selected registry path is");
            A("Other.WinXSortable", "启用 WinX 菜单排序功能", "Enable WinX menu sorting");
            A("Other.ShowFilePath", "状态栏实时显示文件路径", "Show file path in status bar in real time");
            A("Other.OpenMoreRegedit", "注册表编辑器窗口多开", "Allow multiple Registry Editor windows");
            A("Other.OpenMoreExplorer", "文件资源管理器窗口多开", "Allow multiple File Explorer windows");
            A("Other.RestartExplorer", "当前部分操作需要重启文件资源管理器生效", "Some current operations require restarting File Explorer to take effect");
            A("Other.SwitchDictionaries", "切换字典", "Switch Dictionary");
            A("Other.WebDictionaries", "网络字典", "Web Dictionaries");
            A("Other.UserDictionaries", "用户字典", "User Dictionaries");
            A("Other.DictionaryDescription", "字典说明", "Dictionary Description");
            A("Other.GuidInfosDictionary", "GUID信息", "GUID Info");
            A("Other.UwpMode", "UWP模块", "UWP Module");
            A("Other.Translators", "翻译贡献者", "Translation Contributors");
            A("Other.DonationList", "捐赠名单", "Donation List");
            A("Other.ConfigPath", "配置和数据文件保存位置", "Config and data file location");
            A("Other.AppDataDir", "AppData 目录", "AppData Directory");
            A("Other.AppDir", "程序所在目录", "Application Directory");
            A("Other.AutoBackup", "删除菜单时自动备份注册表", "Auto-backup registry when deleting menu items");
            A("Other.SetUpdateFrequency", "设置程序自动检查更新频率", "Set the automatic update check frequency");
            A("Other.OnceAWeek", "每周一次", "Once a week");
            A("Other.OnceAMonth", "每月一次", "Once a month");
            A("Other.OnceASeason", "每季一次", "Once a quarter");
            A("Other.NeverCheck", "从不检查", "Never check");
            A("Other.SetRequestRepo", "设置网络数据访问存储库站点", "Set the network data repository site");
            A("Other.ProtectOpenItem", "保护名称为 \"打开\" 的菜单项目", "Protect menu items named \"Open\"");
            A("Other.WebSearchEngine", "设置网页搜索使用的搜索引擎", "Set the search engine used for web search");
            A("Other.CustomEngine", "自定义", "Custom");
            A("Other.SetCustomEngine", "设置搜索引擎 (以 %s 代替搜索关键词)", "Set the search engine (use %s in place of the keyword)");
            A("Other.HideDisabledItems", "隐藏已禁用的菜单项目", "Hide disabled menu items");
            A("Other.HideSysStoreItems", "隐藏公共引用中的系统菜单", "Hide system menus in public references");
            A("Other.SetPerceivedType", "设置扩展名为 %s 的文件感知类型为", "Set the perceived type of .%s files to");
            A("Other.SetDefaultDropEffect", "设置文件对象默认拖拽命令为", "Set the default drag command for file objects to");
            A("Other.TopMost", "使窗口始终在屏幕最上方", "Keep window always on top");
            A("Other.BundledDictionaries", "内置字典", "Bundled");
            A("Other.DicFileDetail", "{0} · 修改于 {1}", "{0} · modified {1}");
            A("Other.Dictionaries", "程序字典用于为菜单项提供本地化名称、识别 GUID 信息以及提供增强菜单等预设数据。\n字典分为内置字典、网络字典与用户字典，分别位于程序 Assets 目录及配置目录下的 Dictionaries\\Web、Dictionaries\\User 文件夹中。\n下方列出当前各来源已存在的字典文件，可点击“打开文件夹”查看或添加用户字典。", "Dictionaries provide localized names for menu items, identify GUID info, and supply preset data such as enhance menus.\nThey are split into bundled, web and user dictionaries, located in the app's Assets folder and the Dictionaries\\Web and Dictionaries\\User folders under the config directory.\nThe list below shows the dictionary files currently present in each source; use \"Open Folder\" to view or add user dictionaries.");

            // [AboutApp]
            A("AboutApp.Text", "[兼容性能]\n适用于Win10、8.1、8、7、Vista\n适用于 64bit、32bit CPU 操作系统\n适配高分屏，最佳缩放比为150%", "[Compatibility]\nWorks on Win10, 8.1, 8, 7, Vista\nWorks on 64bit and 32bit CPU operating systems\nHigh-DPI aware, best scaling is 150%");

            // [Donate] 捐赠页：中文展示收款码，英文直接跳转仓库
            A("Donate.Title", "感谢您的支持", "Thank you for your support");
            A("Donate.Alipay", "支付宝", "Alipay");
            A("Donate.WeChat", "微信", "WeChat");
            A("Donate.Tip", "如果您觉得本软件有用，欢迎扫码支持作者，感谢您的鼓励！", "If this software helps you, you can support the author by scanning the QR codes above (switch to 简体中文).");
            A("Donate.EnTip", "本项目免费且开源，如需支持作者，请前往 GitHub 仓库：", "This project is free and open-source. To support the author, please visit the GitHub repository:");
        }
    }
}
