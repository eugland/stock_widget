using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WebViewWidget;

public class StockWindowService
{
    private static readonly SettingsService settings = SettingsService.Instance;
    private static readonly StockWindowService _instance = new();

    public static StockWindowService Instance => _instance;

    
    private readonly Dictionary<string, StockWidgetWindow> _stockWindows;

    
    private StockWindowService()
    {

        _stockWindows = new Dictionary<string, StockWidgetWindow>(StringComparer.OrdinalIgnoreCase);
        settings.PortfolioChanged += OnPortfolioChanged;
    }

   
    public StockWidgetWindow ShowStockWindow(string symbol)
    {
        if (_stockWindows.TryGetValue(symbol, out var existingWindow))
        {
            existingWindow.Show();
            existingWindow.Activate();
            return existingWindow;
        }
        else
        {
            var newWindow = new StockWidgetWindow(symbol);
            // The closing logic is now handled here
            newWindow.Closing += (s, e) => { e.Cancel = true; ((Window)s!).Hide(); };
            _stockWindows.Add(symbol, newWindow);
            newWindow.Show();
            newWindow.Activate();
            return newWindow;
        }
    }

    public void ShowAllStockWindows()
    {
        var symbols = (settings.PortfolioSymbols?.Select(q => q.Symbol)
                    ?? Enumerable.Empty<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
        foreach (var symbol in symbols)
        {
            ShowStockWindow(symbol);
        }
        
    }

    public void HideAllStockWindows()
    {
        foreach (var window in _stockWindows.Values)
        {
            window.Hide();
        }
    }

    private void OnPortfolioChanged(object? sender, PortfolioChangedEventArgs e)
    {
        // Ensure we run UI code on the UI thread
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            switch (e.Kind)
            {
                case PortfolioChangeType.Added:
                    // Create/show if missing
                    if (!_stockWindows.TryGetValue(e.Symbol, out var win) || win is null)
                    {
                        win = ShowStockWindow(e.Symbol);
                        _stockWindows[e.Symbol] = win;
                    }
                    win.Show();
                    // If you want it at the back initially, StockWidgetWindow already does SendToBack() on Loaded
                    break;

                case PortfolioChangeType.Removed:
                    if (_stockWindows.TryGetValue(e.Symbol, out var toClose) && toClose is not null)
                    {
                        // Remove your “hide on Closing” override so we can actually close & free resources
                        toClose.Close(); // dispose webviews/timers, etc.
                        _stockWindows.Remove(e.Symbol);
                    }
                    break;
            }
        });
    }

}