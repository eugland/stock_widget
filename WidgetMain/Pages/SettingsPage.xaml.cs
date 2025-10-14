// Pages/SettingsPage.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Linq;
using WidgetMain.Models;
using WidgetMain.Services;

namespace WidgetMain.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = new();

    public SettingsPage()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        await SettingsService.Instance.SaveAsync();
        // Optionally: apply live changes to the shell/window here
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ResetToDefaults();
    }

    private void AddTicker_Click(object sender, RoutedEventArgs e)
    {
        var t = (NewTickerBox.Text ?? "").Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(t) && !ViewModel.Settings.Tickers.Contains(t))
            ViewModel.Settings.Tickers.Add(t);
        NewTickerBox.Text = "";
    }

    private void RemoveTicker_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is string sym)
            ViewModel.Settings.Tickers.Remove(sym);
    }

    private void HotkeyBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Very simple capture: Ctrl/Shift/Alt + Key
        string mod = "";
        if ((Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0) mod += "Ctrl+";
        if ((Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0) mod += "Shift+";
        if ((Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Menu) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0) mod += "Alt+";
        if (e.Key >= Windows.System.VirtualKey.A && e.Key <= Windows.System.VirtualKey.Z)
            ViewModel.Settings.Hotkey = $"{mod}{e.Key}";
        e.Handled = true;
    }
}

public sealed class SettingsViewModel
{
    public AppSettings Settings => SettingsService.Instance.Settings;

    public void ResetToDefaults()
    {
        SettingsService.Instance.Settings = new AppSettings();
    }
}
