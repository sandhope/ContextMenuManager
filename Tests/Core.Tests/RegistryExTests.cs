using ContextMenuManager.Core;
using Microsoft.Win32;
using Xunit;

namespace ContextMenuManager.Core.Tests;

/// <summary>
/// RegistryEx 纯字符串/路径解析逻辑测试。
/// 仅覆盖不触碰真实注册表的解析方法（路径拆分与根项映射）。
/// </summary>
public class RegistryExTests
{
    private const string SamplePath = @"HKEY_CLASSES_ROOT\Directory\shell\MyItem";

    [Fact]
    public void GetParentPath_ReturnsEverythingBeforeLastSeparator()
    {
        Assert.Equal(@"HKEY_CLASSES_ROOT\Directory\shell", RegistryEx.GetParentPath(SamplePath));
    }

    [Fact]
    public void GetKeyName_ReturnsLastSegment()
    {
        Assert.Equal("MyItem", RegistryEx.GetKeyName(SamplePath));
    }

    [Fact]
    public void GetRootName_ReturnsFirstSegment()
    {
        Assert.Equal("HKEY_CLASSES_ROOT", RegistryEx.GetRootName(SamplePath));
    }

    [Fact]
    public void GetPathWithoutRoot_ReturnsEverythingAfterRoot()
    {
        Assert.Equal(@"Directory\shell\MyItem", RegistryEx.GetPathWithoutRoot(SamplePath));
    }

    [Theory]
    [InlineData("HKEY_CLASSES_ROOT", RegistryHive.ClassesRoot)]
    [InlineData("HKCR", RegistryHive.ClassesRoot)]
    [InlineData("HKEY_CURRENT_USER", RegistryHive.CurrentUser)]
    [InlineData("HKCU", RegistryHive.CurrentUser)]
    [InlineData("HKEY_LOCAL_MACHINE", RegistryHive.LocalMachine)]
    [InlineData("HKLM", RegistryHive.LocalMachine)]
    [InlineData("HKEY_USERS", RegistryHive.Users)]
    [InlineData("HKU", RegistryHive.Users)]
    [InlineData("HKEY_CURRENT_CONFIG", RegistryHive.CurrentConfig)]
    [InlineData("HKCC", RegistryHive.CurrentConfig)]
    public void GetRootAndSubRegPath_MapsAllKnownRoots(string root, RegistryHive expectedHive)
    {
        RegistryEx.GetRootAndSubRegPath(root + @"\Some\Sub", out var hive, out var subPath);

        Assert.Equal(expectedHive, hive);
        Assert.Equal(@"Some\Sub", subPath);
    }

    [Fact]
    public void GetRootAndSubRegPath_RootIsCaseInsensitive()
    {
        RegistryEx.GetRootAndSubRegPath(@"hkcr\Directory", out var hive, out var subPath);

        Assert.Equal(RegistryHive.ClassesRoot, hive);
        Assert.Equal("Directory", subPath);
    }

    [Fact]
    public void GetRootAndSubRegPath_RootOnly_ProducesEmptySubPath()
    {
        RegistryEx.GetRootAndSubRegPath("HKEY_LOCAL_MACHINE", out var hive, out var subPath);

        Assert.Equal(RegistryHive.LocalMachine, hive);
        Assert.Equal(string.Empty, subPath);
    }

    [Fact]
    public void GetRootAndSubRegPath_UnknownRoot_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => RegistryEx.GetRootAndSubRegPath(@"HKEY_BOGUS\X", out _, out _));
    }
}
