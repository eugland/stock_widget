using OoplesFinance.YahooFinanceAPI.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Application = System.Windows.Application;

namespace WebViewWidget;

public enum PortfolioChangeType
{
    Added,
    Removed,
    Updated,
    Cleared
}

public sealed class PortfolioChangedEventArgs(PortfolioChangeType kind, string symbol) : EventArgs
{
    public PortfolioChangeType Kind { get; } = kind;
    public string Symbol { get; } = symbol;
}

public sealed class SettingsService : INotifyPropertyChanged
{
    private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
    public static SettingsService Instance => _instance.Value;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<PortfolioChangedEventArgs>? PortfolioChanged;

    private readonly string _filePath;
    private readonly Dictionary<string, object> _settings = new(StringComparer.OrdinalIgnoreCase);

    public List<AutoCompleteResult> PortfolioSymbols
    {
        get => _settings.TryGetValue(nameof(PortfolioSymbols), out var value) && value is List<AutoCompleteResult> list
            ? list
            : [];
        private set
        {
            _settings[nameof(PortfolioSymbols)] = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public string Language
    {
        get
        {
            if (_settings.TryGetValue(nameof(Language), out var value) && value is string lang && !string.IsNullOrWhiteSpace(lang)) return lang;
            string systemLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            Debug.WriteLine($"Got System Lang {systemLang}");
            if (!new[] { "en", "zh", "ja", "ko", "es" }.Contains(systemLang))
                systemLang = "en";

            _settings[nameof(Language)] = systemLang;
            SaveSettings();

            return systemLang;
        }
        set
        {
            if (Language == value) return;
            _settings[nameof(Language)] = value;
            SaveSettings();
            RestartApplication("settings");
        }
    }

    private SettingsService()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolderPath = Path.Combine(appDataPath, "WebViewWidget");
        _filePath = Path.Combine(appFolderPath, "settings.json");

        LoadSettings();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(_filePath))
                return;

            string json = File.ReadAllText(_filePath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (dict == null) return;

            foreach (var kvp in dict)
            {
                if (kvp.Key == nameof(PortfolioSymbols))
                {
                    try
                    {
                        var portfolio = kvp.Value.Deserialize<List<AutoCompleteResult>>();
                        if (portfolio != null)
                            _settings[nameof(PortfolioSymbols)] = portfolio;
                    }
                    catch { }
                }
                else if (kvp.Value.ValueKind == JsonValueKind.String)
                {
                    _settings[kvp.Key] = kvp.Value.GetString()!;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            _settings.Clear();
        }
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    public void AddStock(AutoCompleteResult symbol)
    {
        var list = PortfolioSymbols;
        if (list.Any(s => s.Symbol.Equals(symbol.Symbol, StringComparison.OrdinalIgnoreCase)))
            return;

        list.Add(symbol);
        PortfolioSymbols = list;
        PortfolioChanged?.Invoke(this, new PortfolioChangedEventArgs(PortfolioChangeType.Added, symbol.Symbol));
    }

    public void RemoveStock(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol)) return;

        var list = PortfolioSymbols;
        list.RemoveAll(s => string.Equals(s.Symbol, symbol, StringComparison.OrdinalIgnoreCase));
        PortfolioSymbols = list;
        PortfolioChanged?.Invoke(this, new PortfolioChangedEventArgs(PortfolioChangeType.Removed, symbol));
    }

    public void SetSetting<T>(string key, T value)
    {
        _settings[key] = value!;
        OnPropertyChanged(key);
        SaveSettings();
    }

    public T GetSetting<T>(string key, T defaultValue = default!)
    {
        if (_settings.TryGetValue(key, out var value) && value is JsonElement je)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(je.GetRawText()) ?? defaultValue;
            }
            catch { }
        }

        if (_settings.TryGetValue(key, out var obj) && obj is T typed)
            return typed;

        return defaultValue;
    }

    private static void RestartApplication(string route = "default")
    {
        // Get the running exe path
        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!;

        // Start a new instance
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"--route={route}",
            UseShellExecute = true
        });

        // Kill the current instance cleanly
        Application.Current.Shutdown();
    }
}
