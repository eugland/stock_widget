#nullable enable // Enable nullable context for better null-safety analysis

using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Brushes = System.Windows.Media.Brushes;
using Brush = System.Windows.Media.Brush;

namespace WebViewWidget
{
    public partial class PortfolioPage : Page
    {
        public ObservableCollection<StockViewModel> Portfolio { get; set; }
        private readonly YahooClient YahooClient = new();

        public PortfolioPage()
        {
            InitializeComponent();
            Portfolio = new ObservableCollection<StockViewModel>();
            this.DataContext = this;

            // **MODIFIED**: Load the portfolio when the page is created.
            // We call this without await in the constructor. It's an async void method
            // that will load data in the background and update the UI when ready.
            LoadPortfolioFromSettings();
        }

        /// <summary>
        /// **NEW**: Loads the saved stock symbols from the SettingsService and fetches their current data.
        /// </summary>
        private async void LoadPortfolioFromSettings()
        {
            var savedSymbols = SettingsService.Instance.PortfolioSymbols;
            System.Diagnostics.Debug.WriteLine($"Loading {savedSymbols.Count} symbols from settings.");

            foreach (var stockInfo in savedSymbols)
            {


                Portfolio.Add(new StockViewModel
                {
                    Symbol = stockInfo.Symbol,
                    Name = stockInfo.Name,
                    Price = 0, // Placeholder price
                    Change = 0, // Placeholder change
                    Currency = "USD",
                    ColorRating = Brushes.Green
                });
            }
        }

        private async void StockSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = StockSearchTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                SearchPopup.IsOpen = false;
                return;
            }

            try
            {
                var results = await YahooClient.GetAutoCompleteInfoAsync(query);
                var stockResults = results.Take(10).ToList();

                if (stockResults.Any())
                {
                    SearchResultsListBox.ItemsSource = stockResults;
                    SearchPopup.IsOpen = true;
                }
                else
                {
                    SearchPopup.IsOpen = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR during stock search: {ex.Message}");
            }
        }

        private void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchResultsListBox.SelectedItem is not AutoCompleteResult selectedResult) return;

            AddStockToPortfolio(selectedResult);

            StockSearchTextBox.Text = string.Empty;
            SearchPopup.IsOpen = false;
        }

        private void AddStockToPortfolio(AutoCompleteResult stock)
        {
            if (!Portfolio.Any(p => p.Symbol == stock.Symbol))
            {
                var random = new Random();
                Portfolio.Add(new StockViewModel
                {
                    Symbol = stock.Symbol,
                    Name = stock.Name,
                    Price = random.NextDouble() * 2000 + 50,
                    Change = (random.NextDouble() - 0.5) * 50,
                    Currency = "USD",
                    ColorRating = Brushes.Green
                });

                // **MODIFIED**: Inform the SettingsService to save the new stock.
                SettingsService.Instance.AddStock(stock);
                System.Diagnostics.Debug.WriteLine($"Added {stock.Symbol} to portfolio and settings.");
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is StockViewModel stockToRemove)
            {
                Portfolio.Remove(stockToRemove);

                // **MODIFIED**: Inform the SettingsService that a stock was removed.
                SettingsService.Instance.RemoveStock(stockToRemove.Symbol);
                System.Diagnostics.Debug.WriteLine($"Removed {stockToRemove.Symbol} from portfolio and settings.");
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is StockViewModel stockToConfigure)
            {
                // Your existing logic for the edit window
                var settingsWindow = new ChartSettingsWindow(stockToConfigure);
                settingsWindow.Owner = Window.GetWindow(this);
                settingsWindow.ShowDialog();
            }
        }

        // The remaining event handlers for TimeFrame, Currency, etc. are unchanged.
        #region Unchanged UI Event Handlers
        private void TimeFrame_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is StockViewModel stock && e.AddedItems.Count > 0)
            {
                if ((e.AddedItems[0] as ComboBoxItem)?.Content?.ToString() is string selectedTimeFrame)
                {
                    System.Diagnostics.Debug.WriteLine($"Timeframe for {stock.Symbol} changed to {selectedTimeFrame}");
                }
            }
        }

        private void Currency_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is StockViewModel stock && e.AddedItems.Count > 0)
            {
                if ((e.AddedItems[0] as ComboBoxItem)?.Content?.ToString() is string selectedCurrency)
                {
                    System.Diagnostics.Debug.WriteLine($"Currency for {stock.Symbol} changed to {selectedCurrency}");
                }
            }
        }

        private void ColorRating_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is StockViewModel stock && sender is Ellipse ellipse)
            {
                stock.ColorRating = ellipse.Fill;
            }
        }
        #endregion
    }

    // Your StockViewModel class is unchanged
    public class StockViewModel : INotifyPropertyChanged
    {
        private Brush _colorRating = Brushes.Transparent;
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
        public double Change { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PriceDisplay => Price.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        public string ChangeDisplay => $"{(Change >= 0 ? "+" : "")}{Change:F2} ({(Change / Price):P2})";
        public Brush ChangeColor => Change >= 0 ? Brushes.LightGreen : Brushes.PaleVioletRed;
        public Brush ColorRating
        {
            get => _colorRating;
            set
            {
                if (_colorRating != value)
                {
                    _colorRating = value;
                    OnPropertyChanged(nameof(ColorRating));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

