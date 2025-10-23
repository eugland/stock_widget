using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WebViewWidget.Properties;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace WebViewWidget;

public enum DashboardPageIndex {
    Portfolio = 0,
    Settings = 1
}

public partial class DashboardWindow : Window, INotifyPropertyChanged {
    private string _footerStatus = Strings.Footer_Ready;

    public DashboardWindow(DashboardPageIndex index = DashboardPageIndex.Portfolio) {
        InitializeComponent();
        DataContext = this;
        var items = new List<NavItem> {
            new(Strings.Nav_Portfolio, "\uE7C3", () => ContentFrame.Navigate(new PortfolioPage())), // pie glyph
            new(Strings.Nav_Settings, "\uE713", () => ContentFrame.Navigate(new SettingsPage())) // gear
        };
        NavList.ItemsSource = items;
        SelectTab(index);
        ToastService.Register(this);
    }

    public string FooterStatus {
        get => _footerStatus;
        set {
            _footerStatus = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FooterStatus)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SelectTab(DashboardPageIndex index) {
        NavList.SelectedIndex = (int)index;
    }


    public void navigate_setting() {
        ContentFrame.Navigate(new SettingsPage());
    }

    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (NavList.SelectedItem is NavItem item) item.Action?.Invoke();
    }

    protected override void OnClosing(CancelEventArgs e) {
        // Prevent closing; just hide
        e.Cancel = true;
        Hide();
    }

    public void ShowToast(string message, int ms = 2500) {
        Debug.WriteLine($"[DashBoard] Showing toast: \"{message}\" (Duration={ms}ms)");
        if (string.IsNullOrWhiteSpace(message)) return;

        void CreateAndAnimate() {
            var card = new Border {
                Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x2A, 0x36)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10),
                Opacity = 0,
                RenderTransform = new TranslateTransform(0, 12),
                Child = new TextBlock {
                    Text = message,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 380
                }
            };

            ToastPanel.Children.Add(card);

            // --- IN animations ---
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160))
                { EasingFunction = new QuadraticEase() };

            var slideIn = new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(160))
                { EasingFunction = new QuadraticEase() };

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180))
                { BeginTime = TimeSpan.FromMilliseconds(ms), EasingFunction = new QuadraticEase() };

            var slideOut = new DoubleAnimation(0, 12, TimeSpan.FromMilliseconds(180))
                { BeginTime = TimeSpan.FromMilliseconds(ms), EasingFunction = new QuadraticEase() };

            fadeOut.Completed += (_, __) => ToastPanel.Children.Remove(card);

            card.BeginAnimation(OpacityProperty, fadeIn, HandoffBehavior.Compose);
            (card.RenderTransform as TranslateTransform)!.BeginAnimation(TranslateTransform.YProperty, slideIn,
                HandoffBehavior.Compose);

            card.BeginAnimation(OpacityProperty, fadeOut, HandoffBehavior.Compose);
            (card.RenderTransform as TranslateTransform)!.BeginAnimation(TranslateTransform.YProperty, slideOut,
                HandoffBehavior.Compose);
        }

        // Ensure we’re on the UI thread
        if (!Dispatcher.CheckAccess()) Dispatcher.Invoke(CreateAndAnimate);
        else CreateAndAnimate();
    }

    public record NavItem(string Title, string Icon, Action Action);
}