#nullable disable
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace ContextMenuManager.Core;

/// <summary>纯 IShellLinkW COM 封装，用于创建/读取 .lnk 快捷方式（WinX 添加项需要）。</summary>
public class ShellLink : IDisposable
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, ref WIN32_FIND_DATAW pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotKey(out ushort pwHotkey);
        void SetHotKey(ushort wHotKey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class CShellLink { }

    [ComImport]
    [Guid("45e2b4ae-b1c3-11d0-b92f-00a0c90312e1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellLinkDataList
    {
        void AddDataBlock(IntPtr pDataBlock);
        void CopyDataBlock(uint dwSig, out IntPtr ppDataBlock);
        void RemoveDataBlock(uint dwSig);
        void GetFlags(out ShellLinkDataFlags pdwFlags);
        void SetFlags(ShellLinkDataFlags dwFlags);
    }

    [Flags]
    enum ShellLinkDataFlags : uint
    {
        Default = 0x00000000,
        HasIdList = 0x00000001,
        HasLinkInfo = 0x00000002,
        HasName = 0x00000004,
        HasRelpath = 0x00000008,
        HasWorkingdir = 0x00000010,
        HasArgs = 0x00000020,
        HasIconLocation = 0x00000040,
        Unicode = 0x00000080,
        RunasUser = 0x00002000,
        Valid = 0x07fff7ff,
        Reserved = 0x80000000
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    struct WIN32_FIND_DATAW
    {
        public uint dwFileAttributes;
        public ComTypes.FILETIME ftCreationTime;
        public ComTypes.FILETIME ftLastAccessTime;
        public ComTypes.FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }

    public struct ICONLOCATION
    {
        public string IconPath;
        public int IconIndex;
    }

    private const int MAX_PATH = 260;
    private const int INFOTIPSIZE = 1024;
    private const uint SLGP_UNCPRIORITY = 2;
    private const int STGM_READWRITE = 2;

    private IShellLinkW shellLinkW = null;
    private IPersistFile PersistFile => (IPersistFile)shellLinkW;
    private IShellLinkDataList LinkDataList => (IShellLinkDataList)shellLinkW;

    public string ShortcutPath { get; private set; }

    public string TargetPath
    {
        get
        {
            var targetPath = new StringBuilder(MAX_PATH);
            var data = new WIN32_FIND_DATAW();
            shellLinkW.GetPath(targetPath, targetPath.Capacity, ref data, SLGP_UNCPRIORITY);
            return targetPath.ToString();
        }
        set => shellLinkW.SetPath(value);
    }

    public string Arguments
    {
        get
        {
            var arguments = new StringBuilder(INFOTIPSIZE);
            shellLinkW.GetArguments(arguments, arguments.Capacity);
            return arguments.ToString();
        }
        set => shellLinkW.SetArguments(value);
    }

    public string WorkingDirectory
    {
        get
        {
            var dirPath = new StringBuilder(MAX_PATH);
            shellLinkW.GetWorkingDirectory(dirPath, dirPath.Capacity);
            return dirPath.ToString();
        }
        set => shellLinkW.SetWorkingDirectory(value);
    }

    public ICONLOCATION IconLocation
    {
        get
        {
            var iconPath = new StringBuilder(MAX_PATH);
            shellLinkW.GetIconLocation(iconPath, iconPath.Capacity, out int iconIndex);
            return new ICONLOCATION { IconPath = iconPath.ToString(), IconIndex = iconIndex };
        }
        set => shellLinkW.SetIconLocation(value.IconPath, value.IconIndex);
    }

    public string Description
    {
        get
        {
            var description = new StringBuilder(INFOTIPSIZE);
            shellLinkW.GetDescription(description, description.Capacity);
            return description.ToString();
        }
        set => shellLinkW.SetDescription(value);
    }

    public bool RunAsAdministrator
    {
        get
        {
            LinkDataList.GetFlags(out ShellLinkDataFlags flags);
            return (flags & ShellLinkDataFlags.RunasUser) == ShellLinkDataFlags.RunasUser;
        }
        set
        {
            LinkDataList.GetFlags(out ShellLinkDataFlags flags);
            if(value) flags |= ShellLinkDataFlags.RunasUser;
            else flags &= ~ShellLinkDataFlags.RunasUser;
            LinkDataList.SetFlags(flags);
        }
    }

    public ShellLink(string lnkPath = null)
    {
        try { shellLinkW = (IShellLinkW)new CShellLink(); }
        catch { throw new COMException("Failed to create ShellLink object."); }
        Load(lnkPath);
    }

    ~ShellLink() { Dispose(false); }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if(shellLinkW == null) return;
        Marshal.FinalReleaseComObject(shellLinkW);
        shellLinkW = null;
    }

    public void Save() => Save(this.ShortcutPath);

    public void Save(string lnkPath)
    {
        PersistFile.Save(lnkPath, true);
    }

    public void Load() => Load(this.ShortcutPath);

    public void Load(string lnkPath)
    {
        this.ShortcutPath = lnkPath;
        if(File.Exists(lnkPath)) PersistFile.Load(lnkPath, STGM_READWRITE);
    }
}
