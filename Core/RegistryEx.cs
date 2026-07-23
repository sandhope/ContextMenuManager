#nullable disable
using System;
using System.Security;
using System.Security.AccessControl;
using Microsoft.Win32;

namespace ContextMenuManager.Core
{
    /// <summary>注册表读写扩展（写失败时自动夺取 TrustedInstaller 所有权）</summary>
    public static class RegistryEx
    {
        public const string CLASSES_ROOT = "HKEY_CLASSES_ROOT";
        public const string CURRENT_USER = "HKEY_CURRENT_USER";
        public const string LOCAL_MACHINE = "HKEY_LOCAL_MACHINE";
        public const string CURRENT_CONFIG = "HKEY_CURRENT_CONFIG";
        public const string USERS = "HKEY_USERS";

        public const string HKCR = "HKCR";
        public const string HKCU = "HKCU";
        public const string HKLM = "HKLM";
        public const string HKCC = "HKCC";
        public const string HKU = "HKU";

        public static void CopyTo(this RegistryKey srcKey, RegistryKey dstKey)
        {
            foreach(string name in srcKey.GetValueNames())
            {
                dstKey.SetValue(name, srcKey.GetValue(name), srcKey.GetValueKind(name));
            }
            foreach(string name in srcKey.GetSubKeyNames())
            {
                using(RegistryKey srcSubKey = srcKey.OpenSubKey(name))
                using(RegistryKey dstSubKey = dstKey.CreateSubKey(name))
                    srcSubKey.CopyTo(dstSubKey);
            }
        }

        public static void CopyTo(string srcPath, string dstPath)
        {
            using(RegistryKey srcKey = GetRegistryKey(srcPath))
            using(RegistryKey dstKey = GetRegistryKey(dstPath, true, true))
            {
                CopyTo(srcKey, dstKey);
            }
        }

        public static void MoveTo(this RegistryKey srcKey, RegistryKey dstKey)
        {
            CopyTo(srcKey, dstKey);
            DeleteKeyTree(srcKey.Name, true);
        }

        public static void MoveTo(string srcPath, string dstPath)
        {
            CopyTo(srcPath, dstPath);
            DeleteKeyTree(srcPath, true);
        }

        public static RegistryKey CreateSubKey(this RegistryKey key, string subKeyName, bool writable)
        {
            using(key.CreateSubKey(subKeyName))
                return key.OpenSubKey(subKeyName, writable);
        }

        public static string GetParentPath(string regPath) => regPath.Substring(0, regPath.LastIndexOf('\\'));
        public static string GetKeyName(string regPath) => regPath.Substring(regPath.LastIndexOf('\\') + 1);
        public static string GetRootName(string regPath) => regPath.Substring(0, regPath.IndexOf('\\'));
        public static string GetPathWithoutRoot(string regPath) => regPath.Substring(regPath.IndexOf('\\') + 1);

        public static void DeleteValue(string regPath, string valueName, bool throwOnMissingValue = false)
        {
            try
            {
                using RegistryKey key = GetRegistryKey(regPath, true);
                key?.DeleteValue(valueName, throwOnMissingValue);
            }
            catch(SecurityException ex)
            {
                throw new RegistryAccessException(regPath, true, ex);
            }
        }

        public static void DeleteKeyTree(string regPath, bool throwOnMissingKey = false)
        {
            string dirPath = GetParentPath(regPath);
            string keyName = GetKeyName(regPath);
            try
            {
                try
                {
                    using RegistryKey key = GetRegistryKey(dirPath, true);
                    key?.DeleteSubKeyTree(keyName);
                }
                catch(Exception ex) when(ex is SecurityException || ex is UnauthorizedAccessException)
                {
                    // 目标子树由 TrustedInstaller 保护（父项可开但子项无删除权）：夺取整棵子树所有权后重试删除。
                    RegTrustedInstaller.TakeRegTreeOwnerShip(regPath);
                    using RegistryKey key = GetRegistryKey(dirPath, true);
                    key?.DeleteSubKeyTree(keyName);
                }
            }
            catch(Exception ex) when(ex is SecurityException || ex is UnauthorizedAccessException)
            {
                throw new RegistryAccessException(regPath, true, ex);
            }
            catch(Exception)
            {
                if(throwOnMissingKey) throw;
            }
        }

        /// <summary>
        /// 写入注册表值；无写权限时抛出 RegistryAccessException(NeedsElevation=true) 而非未处理 SecurityException。
        /// </summary>
        public static void SetValue(string regPath, string valueName, object value, RegistryValueKind kind = RegistryValueKind.String)
        {
            try
            {
                using RegistryKey key = GetRegistryKey(regPath, true);
                if(key == null) throw new RegistryAccessException(regPath, true);
                key.SetValue(valueName, value, kind);
            }
            catch(SecurityException ex)
            {
                throw new RegistryAccessException(regPath, true, ex);
            }
        }

        public static void GetRootAndSubRegPath(string regPath, out RegistryHive root, out string subRegPath)
        {
            string rootPath;
            int index = regPath.IndexOf('\\');
            if(index > 0)
            {
                rootPath = regPath.Substring(0, index).ToUpperInvariant();
                subRegPath = regPath.Substring(index + 1);
            }
            else
            {
                rootPath = regPath;
                subRegPath = string.Empty;
            }
            switch(rootPath)
            {
                case HKCR:
                case CLASSES_ROOT:
                    root = RegistryHive.ClassesRoot;
                    break;
                case HKCU:
                case CURRENT_USER:
                    root = RegistryHive.CurrentUser;
                    break;
                case HKLM:
                case LOCAL_MACHINE:
                    root = RegistryHive.LocalMachine;
                    break;
                case HKU:
                case USERS:
                    root = RegistryHive.Users;
                    break;
                case HKCC:
                case CURRENT_CONFIG:
                    root = RegistryHive.CurrentConfig;
                    break;
                default:
                    throw new ArgumentException("Unrecognized registry root: " + regPath);
            }
        }

        public static RegistryKey GetRegistryKey(string regPath, bool writable = false, bool create = false)
        {
            GetRootAndSubRegPath(regPath, out RegistryHive hive, out string keyPath);
            using(RegistryKey root = RegistryKey.OpenBaseKey(hive, RegistryView.Default))
            {
                if(create) return root.CreateSubKey(keyPath, writable);
                if(writable)
                {
                    try
                    {
                        // 常规路径：键不存在返回 null，可写打开成功返回句柄。
                        return root.OpenSubKey(keyPath, true);
                    }
                    catch(SecurityException)
                    {
                        // 无写权限（多为 TrustedInstaller 占有）：夺取整棵子树所有权后重试；仍失败则由调用方处理。
                        RegTrustedInstaller.TakeRegTreeOwnerShip(regPath);
                        return root.OpenSubKey(keyPath, true);
                    }
                }
                return root.OpenSubKey(keyPath, false);
            }
        }

        public static RegistryKey GetRegistryKey(string regPath, RegistryKeyPermissionCheck check, RegistryRights rights)
        {
            GetRootAndSubRegPath(regPath, out RegistryHive hive, out string keyPath);
            using(RegistryKey root = RegistryKey.OpenBaseKey(hive, RegistryView.Default))
                return root.OpenSubKey(keyPath, check, rights);
        }
    }
}
