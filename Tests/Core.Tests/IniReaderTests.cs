using System.Text;
using ContextMenuManager.Core;
using Xunit;

namespace ContextMenuManager.Core.Tests;

/// <summary>IniReader 解析逻辑测试（使用 StringBuilder 输入，避免文件 I/O）。</summary>
public class IniReaderTests
{
    private static IniReader Load(string content) => new IniReader(new StringBuilder(content));

    private const string Sample =
        "; 这是注释\r\n" +
        "# 也是注释\r\n" +
        "[Section1]\r\n" +
        "Key1 = Value1\r\n" +
        "Key2=Value2\r\n" +
        "[Section2]\r\n" +
        "KeyA = A\r\n" +
        "游离行没有等号会被忽略\r\n";

    [Fact]
    public void Parse_ReadsSectionsAndKeys()
    {
        var ini = Load(Sample);

        Assert.Equal(new[] { "Section1", "Section2" }, ini.Sections);
        Assert.Equal("Value1", ini.GetValue("Section1", "Key1"));
        Assert.Equal("Value2", ini.GetValue("Section1", "Key2"));
        Assert.Equal("A", ini.GetValue("Section2", "KeyA"));
    }

    [Fact]
    public void Parse_TrimsWhitespaceAdjacentToSeparator()
    {
        // StringBuilder 输入不会整行 Trim（区别于 LoadFile）：
        // 仅对 '=' 左侧键做 TrimEnd、右侧值做 TrimStart。
        var ini = Load("[S]\r\nKey   =   Value\r\n");

        Assert.Equal("Value", ini.GetValue("S", "Key"));
    }

    [Fact]
    public void GetValue_MissingSectionOrKey_ReturnsEmptyString()
    {
        var ini = Load(Sample);

        Assert.Equal(string.Empty, ini.GetValue("NoSuchSection", "Key1"));
        Assert.Equal(string.Empty, ini.GetValue("Section1", "NoSuchKey"));
    }

    [Fact]
    public void TryGetValue_ReflectsPresence()
    {
        var ini = Load(Sample);

        Assert.True(ini.TryGetValue("Section1", "Key1", out var value));
        Assert.Equal("Value1", value);
        Assert.False(ini.TryGetValue("Section1", "Missing", out var empty));
        Assert.Equal(string.Empty, empty);
    }

    [Fact]
    public void Keys_AreCaseInsensitive()
    {
        var ini = Load(Sample);

        Assert.Equal("Value1", ini.GetValue("section1", "key1"));
    }

    [Fact]
    public void GetSectionKeys_ReturnsKeys_OrNullWhenMissing()
    {
        var ini = Load(Sample);

        Assert.Equal(new[] { "Key1", "Key2" }, ini.GetSectionKeys("Section1"));
        Assert.Null(ini.GetSectionKeys("NoSuchSection"));
    }

    [Fact]
    public void AddValue_AddsAndUpdates()
    {
        var ini = Load(Sample);

        ini.AddValue("Section1", "Key3", "V3");
        Assert.Equal("V3", ini.GetValue("Section1", "Key3"));

        ini.AddValue("Section1", "Key1", "Updated");
        Assert.Equal("Updated", ini.GetValue("Section1", "Key1"));

        ini.AddValue("NewSection", "K", "V");
        Assert.Equal("V", ini.GetValue("NewSection", "K"));
    }

    [Fact]
    public void Remove_SectionAndKey()
    {
        var ini = Load(Sample);

        Assert.True(ini.RemoveKey("Section1", "Key1"));
        Assert.Equal(string.Empty, ini.GetValue("Section1", "Key1"));
        Assert.False(ini.RemoveKey("Section1", "Key1"));

        Assert.True(ini.RemoveSection("Section2"));
        Assert.DoesNotContain("Section2", ini.Sections);
    }

    [Fact]
    public void DuplicateSection_IsIgnored()
    {
        var ini = Load("[S]\r\nA = 1\r\n[S]\r\nB = 2\r\n");

        Assert.Single(ini.Sections);
        Assert.Equal("1", ini.GetValue("S", "A"));
        // 第二个同名节整体被跳过，其键不会被并入。
        Assert.Equal(string.Empty, ini.GetValue("S", "B"));
    }

    [Fact]
    public void EmptyOrWhitespaceInput_ProducesNoSections()
    {
        Assert.Empty(Load(string.Empty).Sections);
        Assert.Empty(Load("   \r\n\t").Sections);
    }
}
