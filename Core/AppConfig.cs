#nullable disable
using System;
using System.IO;

namespace ContextMenuManager.Core
{
    /// <summary>程序配置（移植自原 WinForms 版 AppConfig，去掉 WinForms 依赖）</summary>
    static class AppConfig
    {
        static AppConfig()
        {
            CreateDirectory();
            ReloadConfig();
        }

        public const string GithubLatest = "https://github.com/sandhope/ContextMenuManager/releases/latest";

        public static readonly string AppDir = AppContext.BaseDirectory.TrimEnd('\\');
        public static readonly string AppConfigDir = $@"{AppDir}\Config";
        public static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ContextMenuManager");
        public static readonly string AppDataConfigDir = $@"{AppDataDir}\Config";
        public static readonly string ConfigDir = Directory.Exists(AppConfigDir) ? AppConfigDir : AppDataConfigDir;
        public static readonly bool SaveToAppDir = ConfigDir == AppConfigDir;
        public static readonly bool IsFirstRun = !Directory.Exists(ConfigDir);
        public static string ConfigIni = $@"{ConfigDir}\Config.ini";
        public static string BackupDir = $@"{ConfigDir}\Backup";
        public static string ProgramsDir = $@"{ConfigDir}\Programs";
        public static string DicsDir = $@"{ConfigDir}\Dictionaries";
        public static string WebDicsDir = $@"{DicsDir}\Web";
        public static string UserDicsDir = $@"{DicsDir}\User";

        public const string ZH_CNINI = "zh-CN.ini";
        public const string GUIDINFOSDICINI = "GuidInfosDic.ini";
        public const string DETAILEDEDITDICXML = "DetailedEditDic.xml";
        public const string ENHANCEMENUSICXML = "EnhanceMenusDic.xml";
        public const string UWPMODEITEMSDICXML = "UwpModeItemsDic.xml";

        private static IniReader ConfigReader => new IniReader(ConfigIni);
        private static IniWriter ConfigWriter => new IniWriter(ConfigIni);

        public static void ReloadConfig() { /* IniReader 每次读最新文件，无需缓存 */ }

        private static string GetGeneralValue(string key) => ConfigReader.GetValue("General", key);
        private static void SetGeneralValue(string key, object value)
        {
            ConfigWriter.SetValue("General", key, value);
        }

        public static string Language
        {
            get => GetGeneralValue("Language");
            set => SetGeneralValue("Language", value);
        }

        public static bool AutoBackup
        {
            get => GetGeneralValue("AutoBackup") != "0";
            set => SetGeneralValue("AutoBackup", value ? 1 : 0);
        }

        public static bool ProtectOpenItem
        {
            get => GetGeneralValue("ProtectOpenItem") != "0";
            set => SetGeneralValue("ProtectOpenItem", value ? 1 : 0);
        }

        public static bool ShowFilePath
        {
            get => GetGeneralValue("ShowFilePath") == "1";
            set => SetGeneralValue("ShowFilePath", value ? 1 : 0);
        }

        public static bool WinXSortable
        {
            get => GetGeneralValue("WinXSortable") == "1";
            set => SetGeneralValue("WinXSortable", value ? 1 : 0);
        }

        public static bool OpenMoreRegedit
        {
            get => GetGeneralValue("OpenMoreRegedit") == "1";
            set => SetGeneralValue("OpenMoreRegedit", value ? 1 : 0);
        }

        public static bool OpenMoreExplorer
        {
            get => GetGeneralValue("OpenMoreExplorer") == "1";
            set => SetGeneralValue("OpenMoreExplorer", value ? 1 : 0);
        }

        public static bool HideDisabledItems
        {
            get => GetGeneralValue("HideDisabledItems") == "1";
            set => SetGeneralValue("HideDisabledItems", value ? 1 : 0);
        }

        public static bool HideSysStoreItems
        {
            get => GetGeneralValue("HideSysStoreItems") != "0";
            set => SetGeneralValue("HideSysStoreItems", value ? 1 : 0);
        }

        public static bool RequestUseGithub
        {
            get
            {
                string value = GetGeneralValue("RequestUseGithub");
                if(!string.IsNullOrEmpty(value)) return value == "1";
                return !CultureInfoIsChinese();
            }
            set => SetGeneralValue("RequestUseGithub", value ? 1 : 0);
        }

        public static int UpdateFrequency
        {
            get
            {
                string value = GetGeneralValue("UpdateFrequency");
                if(int.TryParse(value, out int day))
                {
                    if(day == -1 || day == 7 || day == 90) return day;
                }
                return 30;
            }
            set => SetGeneralValue("UpdateFrequency", value);
        }

        public static bool TopMost
        {
            get => GetGeneralValue("TopMost") == "1";
            set => SetGeneralValue("TopMost", value ? 1 : 0);
        }

        private static bool CultureInfoIsChinese()
        {
            try { return System.Globalization.CultureInfo.CurrentCulture.Name == "zh-CN"; }
            catch { return true; }
        }

        public static void CreateDirectory()
        {
            foreach(string dirPath in new[] { AppDataDir, ConfigDir, ProgramsDir, BackupDir, DicsDir, WebDicsDir, UserDicsDir })
            {
                try { Directory.CreateDirectory(dirPath); }
                catch { }
            }
        }
    }
}
