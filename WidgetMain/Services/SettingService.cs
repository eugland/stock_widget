using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using WidgetMain.Models;
using System;

namespace WidgetMain.Services;

public sealed class SettingsService
{
    public static SettingsService Instance { get; } = new();
    public AppSettings Settings { get; set; } = new();

    const string FileName = "settings.json";

    public async Task InitializeAsync()
    {
        try
        {
            var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(FileName) as StorageFile;
            if (file is null) return;
            using var s = await file.OpenStreamForReadAsync();
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(s);
            if (loaded != null) Settings = loaded;
        }
        catch { /* ignore, use defaults */ }
    }

    public async Task SaveAsync()
    {
        var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
        await using var s = await file.OpenStreamForWriteAsync();
        await JsonSerializer.SerializeAsync(s, Settings, new JsonSerializerOptions { WriteIndented = true });
        await s.FlushAsync();
    }
}
