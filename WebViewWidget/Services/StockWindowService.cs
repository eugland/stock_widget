using System.Windows;
using Application = System.Windows.Application;

namespace WebViewWidget;

public class StockWindowService {
    private static readonly SettingsService settings = SettingsService.SettingsServ;
    private readonly Dictionary<string, StockWidgetWindow> _stockWindows;

    private StockWindowService() {
        _stockWindows = new Dictionary<string, StockWidgetWindow>(StringComparer.OrdinalIgnoreCase);
        settings.PortfolioChanged += OnPortfolioChanged;
    }

    public static StockWindowService Instance { get; } = new();

    private StockWidgetWindow ShowStockWindow(string symbol) {
        if (_stockWindows.TryGetValue(symbol, out var existingWindow)) {
            existingWindow.Show();
            existingWindow.Activate();
            return existingWindow;
        }
        var newWindow = new StockWidgetWindow(symbol);
        newWindow.Closing += (s, e) => {
            e.Cancel = true;
            ((Window)s!).Hide();
        };
        _stockWindows.Add(symbol, newWindow);
        newWindow.Show();
        newWindow.Activate();
        return newWindow;
    }

    public void ShowAllStockWindows() {
        var symbols = settings.PortfolioSymbols.Select(q => q.Symbol)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var symbol in symbols) {
            ShowStockWindow(symbol);
        }
    }

    public void HideAllStockWindows() {
        foreach (var window in _stockWindows.Values) {
            window.Hide();
        }
    }

    private void OnPortfolioChanged(object? sender, PortfolioChangedEventArgs e) {
        Application.Current.Dispatcher.Invoke(() => {
            switch (e.Kind) {
                case PortfolioChangeType.Added:
                    if (!_stockWindows.TryGetValue(e.Symbol, out var win)) {
                        win = ShowStockWindow(e.Symbol);
                        _stockWindows[e.Symbol] = win;
                    }
                    win.Show();
                    break;
                case PortfolioChangeType.Removed:
                    if (_stockWindows.TryGetValue(e.Symbol, out var toClose)) {
                        toClose.Close();
                        _stockWindows.Remove(e.Symbol);
                    }
                    break;
            }
        });
    }
}