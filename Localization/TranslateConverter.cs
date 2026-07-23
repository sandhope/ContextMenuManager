#nullable disable
using System;
using Microsoft.UI.Xaml.Data;

namespace ContextMenuManager.Localization
{
    /// <summary>XAML 绑定用翻译转换器。用法：
    /// Text="{Binding Source={StaticResource S}, Path=Version, Converter={StaticResource Translate}, ConverterParameter='SideBar.File'}"
    /// </summary>
    public sealed partial class TranslateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(parameter is string key) return AppStrings.Instance.Get(key);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
