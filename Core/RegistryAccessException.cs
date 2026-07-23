#nullable disable
using System;

namespace ContextMenuManager.Core
{
    /// <summary>
    /// 注册表访问异常：用于区分"权限不足(需要管理员)"与"其他注册表错误"。
    /// RegistryEx 的写入口在捕获 SecurityException 后转为此异常，便于上层给出友好提示并回滚 UI。
    /// </summary>
    public class RegistryAccessException : Exception
    {
        /// <summary>被拒绝访问的注册表路径</summary>
        public string RegPath { get; }

        /// <summary>是否因权限不足(需要以提升的令牌/管理员身份运行)导致</summary>
        public bool NeedsElevation { get; }

        public RegistryAccessException(string regPath, bool needsElevation)
            : base($"注册表访问被拒绝: {regPath}")
        {
            RegPath = regPath;
            NeedsElevation = needsElevation;
        }

        public RegistryAccessException(string regPath, bool needsElevation, Exception inner)
            : base($"注册表访问被拒绝: {regPath}", inner)
        {
            RegPath = regPath;
            NeedsElevation = needsElevation;
        }
    }
}
