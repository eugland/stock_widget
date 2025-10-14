using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WebViewWidget;

public static class SubscriptionStore
{
    private static readonly string FilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebViewWidget", "subscriptions.json");

    public static List<string> Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new List<string>();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch { return new List<string>(); }
    }

    public static void Save(IEnumerable<string> symbols)
    {
        var dir = Path.GetDirectoryName(FilePath)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(symbols);
        File.WriteAllText(FilePath, json);
    }
}