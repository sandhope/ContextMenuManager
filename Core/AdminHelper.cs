using System.Security.Principal;

namespace ContextMenuManager.Core;

/// <summary>进程权限相关的公共判断。</summary>
public static class AdminHelper
{
    /// <summary>当前进程是否以管理员权限运行。</summary>
    public static bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
