#nullable disable
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace ContextMenuManager.Core
{
    /// <summary>外部程序调用（去掉 WinForms 依赖）</summary>
    static class ExternalProgram
    {
        private const string LastKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Applets\Regedit";

        public static void JumpRegEdit(string regPath, string valueName, bool multi)
        {
            try
            {
                string lastKey = regPath.StartsWith("HKEY_", StringComparison.OrdinalIgnoreCase)
                    ? "Computer\\" + regPath
                    : "Computer\\HKEY_CURRENT_USER\\" + regPath;
                Registry.SetValue(LastKeyPath, "LastKey", lastKey);
            }
            catch { }
            try { Process.Start("regedit.exe"); }
            catch { }
        }

        public static void OpenDirectory(string dirPath)
        {
            if(string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath)) return;
            try { Process.Start("explorer.exe", dirPath); }
            catch { }
        }

        public static void ExportRegistry(string regPath, string exportPath)
        {
            try
            {
                var psi = new ProcessStartInfo("reg.exe", $"export \"{regPath}\" \"{exportPath}\" /y")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using(var p = Process.Start(psi)) p?.WaitForExit();
            }
            catch { }
        }
    }
}
