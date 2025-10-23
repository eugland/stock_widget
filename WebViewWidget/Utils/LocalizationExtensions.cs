using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebViewWidget.Properties;

namespace WebViewWidget.Utils;


public static class LocalizationExtensions
{
    public static string ToLocalizedString(this Enum value)
    {
        var key = $"{value.GetType().Name}_{value}";
        var translated = Strings.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
        Debug.WriteLine($"{key}: {translated}");
        return translated ?? value.ToString();
    }
}


