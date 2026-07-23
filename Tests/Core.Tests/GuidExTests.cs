using ContextMenuManager.Core;
using Xunit;

namespace ContextMenuManager.Core.Tests;

/// <summary>GuidEx.IsGuid / TryParse 的纯逻辑测试（GUID 字符串识别与解析）。</summary>
public class GuidExTests
{
    [Theory]
    [InlineData("{20D04FE0-3AEA-1069-A2D8-08002B30309D}")] // 38 位带花括号
    [InlineData("20D04FE0-3AEA-1069-A2D8-08002B30309D")]   // 36 位无花括号
    [InlineData("{aabbccdd-eeff-0011-2233-445566778899}")] // 小写亦可
    public void IsGuid_ValidForms_ReturnsTrue(string input)
    {
        Assert.True(GuidEx.IsGuid(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-guid")]
    [InlineData("20D04FE0-3AEA-1069-A2D8-08002B30309D}")]  // 缺少左花括号且长度不符
    [InlineData("{20D04FE0-3AEA-1069-A2D8-08002B30309D")]  // 缺少右花括号
    [InlineData("20D04FE0-3AEA-1069-A2D8-08002B3030")]     // 长度不足
    public void IsGuid_InvalidForms_ReturnsFalse(string? input)
    {
        Assert.False(GuidEx.IsGuid(input!));
    }

    [Fact]
    public void TryParse_ValidGuid_ReturnsTrueAndParsedValue()
    {
        bool ok = GuidEx.TryParse("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", out var guid);

        Assert.True(ok);
        Assert.Equal(new Guid("20D04FE0-3AEA-1069-A2D8-08002B30309D"), guid);
    }

    [Fact]
    public void TryParse_InvalidGuid_ReturnsFalseAndEmpty()
    {
        bool ok = GuidEx.TryParse("garbage", out var guid);

        Assert.False(ok);
        Assert.Equal(Guid.Empty, guid);
    }
}
