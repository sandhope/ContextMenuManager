#nullable disable
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ContextMenuManager.Core
{
    /// <summary>间接字符串（@filename,-id 或 ms-resource）解析</summary>
    public static class ResourceString
    {
        [DllImport("shlwapi.dll", BestFitMapping = false, CharSet = CharSet.Unicode,
            ExactSpelling = true, SetLastError = false, ThrowOnUnmappableChar = true)]
        private static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, uint cchOutBuf, IntPtr ppvReserved);

        public static string GetDirectString(string resStr)
        {
            if(string.IsNullOrEmpty(resStr)) return string.Empty;
            StringBuilder outBuff = new StringBuilder(1024);
            SHLoadIndirectString(resStr, outBuff, 1024, IntPtr.Zero);
            return outBuff.ToString();
        }

        public static readonly string OK = GetDirectString("@shell32.dll,-9752");
        public static readonly string Cancel = GetDirectString("@shell32.dll,-9751");
    }
}
