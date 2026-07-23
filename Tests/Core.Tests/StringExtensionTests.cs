using ContextMenuManager.Core;
using Xunit;

namespace ContextMenuManager.Core.Tests;

/// <summary>StringExtension 扩展方法的纯逻辑测试。</summary>
public class StringExtensionTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("\t\r\n", true)]
    [InlineData("a", false)]
    [InlineData(" a ", false)]
    public void IsNullOrWhiteSpace_MatchesFrameworkSemantics(string? input, bool expected)
    {
        Assert.Equal(expected, input!.IsNullOrWhiteSpace());
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" ", false)]
    [InlineData("x", false)]
    public void IsNullOrEmpty_MatchesFrameworkSemantics(string? input, bool expected)
    {
        Assert.Equal(expected, input!.IsNullOrEmpty());
    }

    [Fact]
    public void IsEqual_DefaultsToOrdinalIgnoreCase()
    {
        Assert.True("HKEY_CLASSES_ROOT".IsEqual("hkey_classes_root"));
        Assert.True("Shell".IsEqual("shell"));
    }

    [Fact]
    public void IsEqual_WithOrdinalComparison_IsCaseSensitive()
    {
        Assert.False("Shell".IsEqual("shell", StringComparison.Ordinal));
        Assert.True("Shell".IsEqual("Shell", StringComparison.Ordinal));
    }
}
