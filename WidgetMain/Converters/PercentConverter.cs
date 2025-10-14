// Converters/PercentConverter.cs
using Microsoft.UI.Xaml.Data;
using System;

namespace WidgetMain.Converters;

public sealed class PercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is double d ? $"{d:P0}" : "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return double.TryParse(value?.ToString().TrimEnd('%'), out var d) ? d / 100 : 1.0;
    }
}