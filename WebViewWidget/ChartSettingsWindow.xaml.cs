using System.Windows;
using System.Windows.Controls;

namespace WebViewWidget
{
    public partial class ChartSettingsWindow : Window
    {
        private StockViewModel StockData { get; set; }

        // Constructor that accepts the StockViewModel
        public ChartSettingsWindow(StockViewModel stock)
        {
            InitializeComponent();
            StockData = stock;

            // Set the window title
            Title = $"Chart Configuration for {StockData.Symbol}";

            // Optionally, load current settings into the controls
            // e.g., CurrencyComboBox.SelectedItem = stock.ChartCurrency;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the selected values
            string? selectedCurrency = (CurrencyComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string? selectedDuration = (DurationComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            // 2. Apply the changes to the StockViewModel (you'd need to add these properties)
            // StockData.ChartCurrency = selectedCurrency;
            // StockData.ChartDuration = selectedDuration;

            // 3. Close the window
            DialogResult = true; // Signals successful save
            Close();
        }
    }
}