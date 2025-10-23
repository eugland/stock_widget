// Enable nullable context for better null-safety analysis

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Models;
using Brushes = System.Windows.Media.Brushes;
using Brush = System.Windows.Media.Brush;

namespace WebViewWidget;

public partial class PortfolioPage {
    private readonly YahooClient yf = new();

    public PortfolioPage() {
        InitializeComponent();
        Portfolio = new ObservableCollection<StockViewModel>();
        DataContext = this;
        LoadPortfolioFromSettings();
    }

    public ObservableCollection<StockViewModel> Portfolio { get; set; }


    private async void LoadPortfolioFromSettings() {
        var savedSymbols = SettingsService.Instance.PortfolioSymbols;
        Debug.WriteLine($"Loading {savedSymbols.Count} symbols from settings.");

        foreach (var stockInfo in savedSymbols)
            Portfolio.Add(new StockViewModel {
                Symbol = stockInfo.Symbol,
                Name = stockInfo.Name,
                Price = 0,
                Change = 0,
                Currency = "USD",
                ColorRating = Brushes.Green
            });
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
                results.Where(r => r.Type == "S" || r.Type == "E" || r.Type == "I").Take(10)
                    .ToList(); // Filter Type in [S, E, I]
            Debug.WriteLine(string.Join(", ", stockResults.Select(r => r.Type)));

            if (stockResults.Any()) {
                SearchResultsListBox.ItemsSource = stockResults;
                SearchPopup.IsOpen = true;
            }
            else {
                SearchPopup.IsOpen = false;
            }
        }
        catch (Exception ex) {
            Debug.WriteLine($"ERROR during stock search: {ex.Message}");
        }
    }

    private void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (SearchResultsListBox.SelectedItem is not AutoCompleteResult selectedResult) return;

        AddStockToPortfolio(selectedResult);

        StockSearchTextBox.Text = string.Empty;
        SearchPopup.IsOpen = false;
    }

    private void AddStockToPortfolio(AutoCompleteResult stock) {
        if (!Portfolio.Any(p => p.Symbol == stock.Symbol)) {
            var random = new Random();
            Portfolio.Add(new StockViewModel {
                Symbol = stock.Symbol,
                Name = stock.Name,
                Price = random.NextDouble() * 2000 + 50,
                Change = (random.NextDouble() - 0.5) * 50,
                Currency = "USD",
                ColorRating = Brushes.Green
            });
            SettingsService.Instance.AddStock(stock);
            Debug.WriteLine($"Added {stock.Symbol} to portfolio and settings.");
            ToastService.Show($"{stock.Symbol} added to your Desktop.");
        }
        else {
            ToastService.Show($"{stock.Symbol} is already in your portfolio.");
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e) {
        if ((sender as FrameworkElement)?.DataContext is StockViewModel stockToRemove) {
            Portfolio.Remove(stockToRemove);
            SettingsService.Instance.RemoveStock(stockToRemove.Symbol);

            ToastService.Show($"{stockToRemove.Symbol} Removed.");
            Debug.WriteLine($"Removed {stockToRemove.Symbol} from portfolio and settings.");
        }
    }
}

public class StockViewModel : INotifyPropertyChanged {
    private Brush _colorRating = Brushes.Transparent;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public double Change { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PriceDisplay => Price.ToString("C", CultureInfo.GetCultureInfo("en-US"));
    public string ChangeDisplay => $"{(Change >= 0 ? "+" : "")}{Change:F2} ({Change / Price:P2})";
    public Brush ChangeColor => Change >= 0 ? Brushes.LightGreen : Brushes.PaleVioletRed;

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

    protected virtual void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}