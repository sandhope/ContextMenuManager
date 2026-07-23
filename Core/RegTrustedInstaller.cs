#nullable disable
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ContextMenuManager.Core
{
    /// <summary>
    /// 获取 TrustedInstaller 所有的注册表项的所有权（移植并修正自原 WinForms 版 RegTrustedInstaller）。
    /// 原始算法作者：JPBlanc。
    /// 说明：需进程以管理员身份运行；本类仅在写权限打开失败后被调用，用于将项所有者改为当前管理员并授予完全控制。
    /// </summary>
    public static class RegTrustedInstaller
    {
        private static class NativeMethod
        {
            public const string TakeOwnership = "SeTakeOwnershipPrivilege";
            public const string Restore = "SeRestorePrivilege";

            [StructLayout(LayoutKind.Sequential)]
            public struct LUID
            {
                public int lowPart;
                public int highPart;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct LUID_AND_ATTRIBUTES
            {
                public LUID Luid;
                public int Attributes;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct TOKEN_PRIVILEGES
            {
                public int PrivilegeCount;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
                public LUID_AND_ATTRIBUTES[] Privileges;
            }

            private const int SE_PRIVILEGE_ENABLED = 2;
            private const int SE_PRIVILEGE_DISABLED = 0;

            // 令牌访问权限：这里需要 AdjustPrivileges | Query。
            private const int TOKEN_ADJUST_PRIVILEGES = 32;
            private const int TOKEN_QUERY = 8;

            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool AdjustTokenPrivileges([In] IntPtr accessTokenHandle, [In] bool disableAllPrivileges,
                [In] ref TOKEN_PRIVILEGES newState, [In] int bufferLength, [In, Out] ref TOKEN_PRIVILEGES previousState, [In, Out] ref int returnLength);

            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool LookupPrivilegeValue([In] string systemName, [In] string name, [In, Out] ref LUID luid);

            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool OpenProcessToken([In] IntPtr processHandle, [In] int desiredAccess, [In, Out] ref IntPtr tokenHandle);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern IntPtr GetCurrentProcess();

            /// <summary>为当前进程令牌启用/禁用指定特权。</summary>
            public static bool TrySetPrivilege(string privilege, bool enable)
            {
                TOKEN_PRIVILEGES newTP = new TOKEN_PRIVILEGES();
                TOKEN_PRIVILEGES oldTP = new TOKEN_PRIVILEGES();
                LUID luid = new LUID();
                int returnLength = 0;
                IntPtr processToken = IntPtr.Zero;

                if(!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref processToken)) return false;
                if(!LookupPrivilegeValue(null, privilege, ref luid)) return false;

                newTP.PrivilegeCount = 1;
                newTP.Privileges = new LUID_AND_ATTRIBUTES[64];
                newTP.Privileges[0].Luid = luid;
                newTP.Privileges[0].Attributes = enable ? SE_PRIVILEGE_ENABLED : SE_PRIVILEGE_DISABLED;

                oldTP.PrivilegeCount = 64;
                oldTP.Privileges = new LUID_AND_ATTRIBUTES[64];
                return AdjustTokenPrivileges(processToken, false, ref newTP, 16, ref oldTP, ref returnLength);
            }
        }

        /// <summary>将单个注册表项的所有者改为当前用户并授予完全控制（失败静默）。</summary>
        /// <param name="regPath">注册表完整路径（含根项，如 HKEY_CLASSES_ROOT\...）。</param>
        public static void TakeRegKeyOwnerShip(string regPath)
        {
            if(string.IsNullOrWhiteSpace(regPath)) return;
            RegistryKey key = null;
            WindowsIdentity id = null;
            try
            {
                id = WindowsIdentity.GetCurrent();

                // 启用夺取所有权与恢复特权（更改所有者两者缺一不可），非管理员时会失败并直接返回。
                if(!NativeMethod.TrySetPrivilege(NativeMethod.TakeOwnership, true)) return;
                if(!NativeMethod.TrySetPrivilege(NativeMethod.Restore, true)) return;

                // 以 TakeOwnership 权限打开（此重载不会回调本类，避免递归）。
                key = RegistryEx.GetRegistryKey(regPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.TakeOwnership);
                if(key == null) return;

                RegistrySecurity security = key.GetAccessControl(AccessControlSections.All);
                // 使当前用户成为所有者。
                security.SetOwner(id.User);
                key.SetAccessControl(security);

                // 授予当前用户完全控制（含子项继承），随后即可写入/删除。
                security.AddAccessRule(new RegistryAccessRule(id.User, RegistryRights.FullControl,
                    InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
                key.SetAccessControl(security);
            }
            catch { /* 夺权失败（策略限制/安全软件保护等）时静默，交由上层回退提示 */ }
            finally { key?.Close(); id?.Dispose(); }
        }

        /// <summary>递归夺取注册表项及其所有子项的所有权（删除子树前需对整棵树夺权）。</summary>
        /// <param name="regPath">注册表完整路径（含根项）。</param>
        public static void TakeRegTreeOwnerShip(string regPath)
        {
            if(string.IsNullOrWhiteSpace(regPath)) return;
            TakeRegKeyOwnerShip(regPath);
            try
            {
                using(RegistryKey key = RegistryEx.GetRegistryKey(regPath))
                {
                    if(key == null) return;
                    foreach(string subKeyName in key.GetSubKeyNames())
                    {
                        TakeRegTreeOwnerShip($@"{key.Name}\{subKeyName}");
                    }
                }
            }
            catch { }
        }
    }
}
