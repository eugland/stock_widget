using System.Globalization;
using System.Windows.Controls;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;
using OoplesFinance.YahooFinanceAPI.Models;

namespace DesktopWidget;

public partial class StockWidget : UserControl
{
    private StockWidgetViewModel? _vm;

    public StockWidget()
    {
        InitializeComponent();
        Loaded += (_, __) =>
        {
            _vm = new StockWidgetViewModel("NVDA"); // <-- set your symbol
            DataContext = _vm;
        };
        Unloaded += (_, __) => _vm?.Dispose();
    }
    
}