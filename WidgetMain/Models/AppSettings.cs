using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;

namespace WidgetMain.Models;

public class AppSettings : INotifyPropertyChanged
{
    string _theme = "Default";           // Default | Light | Dark
    Color _accentColor = Color.FromArgb(255, 0, 255, 128);
    int _refreshSeconds = 60;
    double _opacity = 1.0;               // 0.5 – 1.0
    bool _useMica = true;
    bool _showGridLines = false;
    bool _alwaysOnTop = false;
    string _hotkey = "Ctrl+Shift+W";     // e.g., toggle all widgets
    List<string> _tickers = new() { "MSFT", "CRVW" };

    public string Theme { get => _theme; set => Set(ref _theme, value); }
    public Color AccentColor { get => _accentColor; set => Set(ref _accentColor, value); }
    public int RefreshSeconds { get => _refreshSeconds; set => Set(ref _refreshSeconds, value); }
    public double Opacity { get => _opacity; set => Set(ref _opacity, value); }
    public bool UseMica { get => _useMica; set => Set(ref _useMica, value); }
    public bool ShowGridLines { get => _showGridLines; set => Set(ref _showGridLines, value); }
    public bool AlwaysOnTop { get => _alwaysOnTop; set => Set(ref _alwaysOnTop, value); }
    public string Hotkey { get => _hotkey; set => Set(ref _hotkey, value); }
    public List<string> Tickers { get => _tickers; set => Set(ref _tickers, value); }

    public event PropertyChangedEventHandler? PropertyChanged;
    bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
