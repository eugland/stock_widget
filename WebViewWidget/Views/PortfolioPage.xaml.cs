// Enable nullable context for better null-safety analysis

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Models;
using WebViewWidget.Properties;
using Brushes = System.Windows.Media.Brushes;
using Brush = System.Windows.Media.Brush;

namespace WebViewWidget;

public partial class PortfolioPage {
    private readonly YahooClient yf = new();

    public PortfolioPage() {
        InitializeComponent();
        Portfolio = [];
        DataContext = this;
        Loaded += (_, _) => LoadPortfolioFromSettings();
    }

    public ObservableCollection<StockViewModel> Portfolio { get; }

    private async void LoadPortfolioFromSettings() {
        var savedSymbols = SettingsService.SettingsServ.PortfolioSymbols;
        Debug.WriteLine($"Loading {savedSymbols.Count} symbols from settings.");
        foreach (var stockInfo in savedSymbols) {
            Portfolio.Add(new StockViewModel {
                Symbol = stockInfo.Symbol,
                Name = stockInfo.Name
            });
        }
    }

    private async void StockSearchTextBox_TextChanged(object sender, TextChangedEventArgs e) {
        var query = StockSearchTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) {
            SearchPopup.IsOpen = false;
            return;
        }

        try {
            var results = await yf.GetAutoCompleteInfoAsync(query);
            var stockResults =
                results.Where(r => r.Type is "S" or "E" or "I").Take(10)
                    .ToList(); // Filter Type in [S, E, I]
            Debug.WriteLine(string.Join(", ", stockResults.Select(r => r.Type)));

            if (stockResults.Count != 0) {
                SearchResultsListBox.ItemsSource = stockResults;
                SearchPopup.IsOpen = true;
            } else {
                SearchPopup.IsOpen = false;
            }
        } catch (Exception ex) {
            Debug.WriteLine($"ERROR during stock search: {ex.Message}");
        }
    }

    private void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (SearchResultsListBox.SelectedItem is not AutoCompleteResult selectedResult) {
            return;
        }

        AddStockToPortfolio(selectedResult);
        StockSearchTextBox.Text = string.Empty;
        SearchPopup.IsOpen = false;
    }

    private void AddStockToPortfolio(AutoCompleteResult stock) {
        if (Portfolio.Any(p => p.Symbol == stock.Symbol)) {
            ToastService.Show(string.Format(Strings.Toast_AlreadyInPortfolio, stock.Symbol));
            return;
        }
        var random = new Random();
        Portfolio.Add(new StockViewModel {
            Symbol = stock.Symbol,
            Name = stock.Name
        });
        SettingsService.SettingsServ.AddStock(stock);
        Debug.WriteLine($"Added {stock.Symbol} to portfolio and settings.");
        ToastService.Show(string.Format(Strings.Toast_AddedStock, stock.Symbol));
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e) {
        if ((sender as FrameworkElement)?.DataContext is StockViewModel stockToRemove) {
            Portfolio.Remove(stockToRemove);
            SettingsService.SettingsServ.RemoveStock(stockToRemove.Symbol);
            ToastService.Show($"{stockToRemove.Symbol} Removed.");
            Debug.WriteLine($"Removed {stockToRemove.Symbol} from portfolio and settings.");
        }
    }
}

public sealed class StockViewModel : INotifyPropertyChanged {
    private Brush _colorRating = Brushes.Transparent;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Brush ColorRating {
        get => _colorRating;
        set {
            if (_colorRating != value) {
                _colorRating = value;
                OnPropertyChanged(nameof(ColorRating));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}