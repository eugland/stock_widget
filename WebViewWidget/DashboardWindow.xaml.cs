using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

// ... (other using statements)
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;

namespace WebViewWidget
{
    public partial class DashboardWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // ... (FooterStatus property and backing field remain the same) ...
        public string FooterStatus
        {
            get => _footerStatus;
            set { _footerStatus = value; PropertyChanged?.Invoke(this, new(nameof(FooterStatus))); }
        }
        private string _footerStatus = "Ready";

        public record NavItem(string Title, string Icon, Action Action);

        public DashboardWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Build nav
            var items = new List<NavItem>
            {
                new("Portfolio", "\uE7C3", () => ContentFrame.Navigate((new PortfolioPage())) ), // pie glyph
                new("Settings",  "\uE713", () => ContentFrame.Navigate((new SettingsPage())) ),  // gear
            };
            NavList.ItemsSource = items;
            NavList.SelectedIndex = 0;
        }

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavList.SelectedItem is NavItem item)
            {
                item.Action?.Invoke();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Prevent closing; just hide
            e.Cancel = true;
            this.Hide();
        }
    }
}