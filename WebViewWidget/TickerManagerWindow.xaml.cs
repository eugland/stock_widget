using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace WebViewWidget
{
    public partial class TickerManagerWindow : Window
    {
        private readonly WidgetManager _manager;
        private readonly List<string> _symbols;

        public TickerManagerWindow(WidgetManager manager)
        {
            InitializeComponent();
            _manager = manager;
            _symbols = SubscriptionStore.Load();
            RefreshList();
        }

        private void RefreshList()
        {
            SymbolsList.ItemsSource = null;
            SymbolsList.ItemsSource = _symbols.OrderBy(s => s).ToList();
        }

        private static string Normalize(string s) => (s ?? "").Trim().ToUpperInvariant();
        
        // Title bar drag + window buttons
        private void TitleBar_Drag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }
        private void Min_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close(); // your OnClosing hides to tray


        private IEnumerable<string> GetSelectedSymbols()
        {
            // Works for both Single and Extended modes
            var multi = SymbolsList.SelectedItems.Cast<string>().ToList();
            if (multi.Count > 0) return multi;
            if (SymbolsList.SelectedItem is string one) return new[] { one };
            return Enumerable.Empty<string>();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var s = Normalize(SymbolBox.Text);
            if (string.IsNullOrWhiteSpace(s)) return;
            if (!_symbols.Contains(s)) _symbols.Add(s);
            SymbolBox.Clear();
            RefreshList();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SubscriptionStore.Save(_symbols);
            MessageBox.Show("Saved.", "Subscriptions", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Launch_Click(object sender, RoutedEventArgs e)
        {
            var picked = GetSelectedSymbols().ToList();
            if (!picked.Any())
            {
                MessageBox.Show("Select one or more symbols to launch.", "No selection",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (var s in picked)
                _manager.EnsureWidget(s);
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            var picked = GetSelectedSymbols().ToList();
            if (!picked.Any()) return;

            foreach (var s in picked)
                _symbols.Remove(s);

            RefreshList();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // hide to tray
            Hide();
            base.OnClosing(e);
        }
    }
}
