#nullable disable
using System;

namespace ContextMenuManager.Core
{
    /// <summary>字符串扩展（常用方法）</summary>
    public static class StringExtension
    {
        public static bool IsNullOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);

        public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);

        public static bool IsEqual(this string a, string b, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
            => string.Equals(a, b, comparison);
    }
}
