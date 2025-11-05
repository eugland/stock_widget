// WebViewWidget.NativeWindow/MainWindow.xaml.cs

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;
using WebViewWidget.Utils;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Timer = System.Timers.Timer;

namespace WebViewWidget;

public partial class StockWidgetWindow : INotifyPropertyChanged {
    private static readonly IntPtr HWND_BOTTOM = new(1);
    private readonly string _symbol = "CRWV";
    private readonly Timer _timer = new(60_000);
    private readonly YahooClient _yahoo = new();
    private Button? _activeButton;
    private List<Point> _chartPoints = new();

    private string _currencySymbol = "$";
    private bool _isFetching;
    private double _prevPrice;
    private double _price;
    private string _rangeLabel = "1D • 2m";
    private string _updatedDisplay = "";

    public StockWidgetWindow(string ticker) {
        Symbol = ticker;
        InitializeComponent();
        DataContext = this;

        RootGrid.MouseLeftButtonDown += (obj, e) => {
            if (IsOnInteractiveElement(e.OriginalSource as DependencyObject)) {
                return;
            }

            try {
                DragMove();
            } catch (Exception ex) {
                Debug.WriteLine($"[DEBUG] DragMove exception: {ex.Message}");
            }
        };
        Loaded += (_, __) => SendToBack();
        _ = RefreshFromYahooAsync();
        _timer.Elapsed += async (_, _) => await RefreshFromYahooAsync();
        _timer.AutoReset = true;
        _timer.Start();
        setSelectedDateButton(Btn1D);
        Debug.WriteLine("starting " + ticker);
    }

    private TimeRange SelectedTimeRange { get; set; } = TimeRange._1Day;
    private TimeInterval SelectedTimeInterval { get; set; } = TimeInterval._2Minutes;

    public string Symbol {
        get => _symbol;
        init {
            _symbol = value;
            OnChanged(nameof(Symbol));
        }
    }

    public string RangeLabel {
        get => _rangeLabel;
        set {
            _rangeLabel = value;
            OnChanged(nameof(RangeLabel));
        }
    }

    public double Price {
        get => _price;
        set {
            _price = value;
            OnChanged(nameof(Price));
            OnChanged(nameof(ChangeDisplay));
        }
    }

    public double PrevPrice {
        get => _prevPrice;
        set {
            _prevPrice = value;
            OnChanged(nameof(ChangeDisplay));
        }
    }

    public string ChangeDisplay {
        get {
            var change = Price - PrevPrice;
            var pct = PrevPrice != 0 ? change / PrevPrice * 100 : 0;
            var sign = change >= 0 ? "+" : "-";
            return $"{sign}{CurrencySymbol}{Math.Abs(change):F2} ({sign}{Math.Abs(pct):F2}%)";
        }
    }

    public string CurrencySymbol {
        get => _currencySymbol;
        set {
            _currencySymbol = value;
            OnChanged(nameof(CurrencySymbol));
            OnChanged(nameof(ChangeDisplay)); // refresh formatted change
        }
    }

    public string UpdatedDisplay {
        get => _updatedDisplay;
        set {
            _updatedDisplay = value;
            OnChanged(nameof(UpdatedDisplay));
        }
    }

    public List<Point> ChartPoints {
        get => _chartPoints;
        set {
            _chartPoints = value;
            OnChanged(nameof(ChartPoints));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private static string GetCurrencySymbol(string? code) {
        return code?.ToUpper() switch {
            // North America
            "USD" => "$",
            "CAD" => "CA$",
            "MXN" => "MX$",

            // Europe
            "EUR" => "€",
            "GBP" => "£",
            "CHF" => "CHF",
            "NOK" => "kr",
            "SEK" => "kr",
            "DKK" => "kr",
            "PLN" => "zł",
            "CZK" => "Kč",
            "HUF" => "Ft",
            "RON" => "lei",

            // Asia
            "JPY" => "¥",
            "CNY" => "¥",
            "HKD" => "HK$",
            "TWD" => "NT$",
            "KRW" => "₩",
            "SGD" => "S$",
            "THB" => "฿",
            "INR" => "₹",
            "IDR" => "Rp",
            "MYR" => "RM",
            "PHP" => "₱",
            "VND" => "₫",

            // Oceania
            "AUD" => "A$",
            "NZD" => "NZ$",
            "FJD" => "FJ$",

            // Middle East / Africa
            "ILS" => "₪",
            "AED" => "د.إ",
            "SAR" => "﷼",
            "TRY" => "₺",
            "EGP" => "E£",
            "ZAR" => "R",
            "NGN" => "₦",
            "KES" => "KSh",

            // South America
            "BRL" => "R$",
            "ARS" => "$",
            "CLP" => "CLP$",
            "COP" => "COL$",
            "PEN" => "S/",
            "UYU" => "$U",
            "BOB" => "Bs",

            // Crypto (common tickers)
            "BTC" => "₿",
            "ETH" => "Ξ",
            "USDT" => "₮",
            "USDC" => "₮",
            "SOL" => "◎",
            "BNB" => "🟡",
            "DOGE" => "Ð",
            "XRP" => "✕",
            "ADA" => "₳",

            // Default fallback
            _ => code + " ¤" // fallback: ISO code + generic currency sign
        };
    }

    public void ChangeTimeRange(TimeRange range, TimeInterval interval) {
        SelectedTimeRange = range;
        SelectedTimeInterval = interval;
        _ = RefreshFromYahooAsync();
    }

    private void setSelectedDateButton(Button button) {
        if (_activeButton != null) {
            _activeButton.FontWeight = FontWeights.Regular;
            _activeButton.Foreground = new SolidColorBrush(Color.FromRgb(169, 177, 187)); // default gray
            _activeButton.Background = Brushes.Transparent;
        }

        button.Foreground = new SolidColorBrush(Color.FromRgb(86, 182, 247));
        button.FontWeight = FontWeights.Bold;
        button.Background = new SolidColorBrush(Color.FromArgb(40, 86, 182, 247));
        _activeButton = button;
    }

    private void TimeRangeButton_Click(object sender, RoutedEventArgs e) {
        if (sender is not Button { Tag: string tag } button) {
            return;
        }

        var parts = tag.Split(',');
        if (parts.Length != 2) {
            return;
        }

        if (!Enum.TryParse<TimeRange>(parts[0], out var range) ||
            !Enum.TryParse<TimeInterval>(parts[1], out var interval)) {
            return;
        }

        Debug.WriteLine($"[DEBUG] Parsed TimeRange={range}, TimeInterval={interval}. Invoking ChangeTimeRange.");
        ChangeTimeRange(range, interval);

        if (_activeButton != null) {
            _activeButton.Foreground = new SolidColorBrush(Color.FromRgb(169, 177, 187)); // #A9B1BB
        }

        setSelectedDateButton(button);
    }

    private void ToggleAlwaysOnTop_Click(object sender, RoutedEventArgs e) {
        if (Topmost) {
            AlwaysOnTopToggle.IsChecked = false;
            Topmost = false;
            SendToBack();
        } else {
            AlwaysOnTopToggle.IsChecked = true;
            Topmost = true;
        }
    }

    private static bool IsOnInteractiveElement(DependencyObject? d) {
        while (d != null) {
            d = VisualTreeHelper.GetParent(d);
        }

        return false;
    }

    private async Task RefreshFromYahooAsync() {
        if (_isFetching) {
            return;
        }

        _isFetching = true;

        try {
            var chartRoot = await _yahoo.GetChartResults(Symbol, SelectedTimeRange, SelectedTimeInterval);
            _prevPrice = chartRoot.Chart.Result[0].Indicators?.Quote[0].Open?[0] ?? 0;
            var meta1 = chartRoot.Chart.Result[0].Meta;
            var currency = meta1?.Currency;

            if (meta1?.ChartPreviousClose is { } prevClose) {
                _prevPrice = prevClose;
            }


            var closer = chartRoot.Chart.Result[0].Indicators?.Quote[0].Close;
            if (closer is null) {
                return;
            }

            var startTime = DateTime.Now;
            var points = closer
                .Select((p, i) => new { Value = p, Index = i })
                .Where(x => x.Value.HasValue) // keep only non-null values
                .Select(x => new Point(x.Index, x.Value!.Value))
                .ToList();
            var endTime = DateTime.Now;
            Debug.WriteLine(
                $"[DEBUG] Total points: {points.Count}, Duration: {(endTime - startTime).TotalMilliseconds} ms");

            var first = points.First().Y;
            var last = points.Last().Y;

            await Dispatcher.InvokeAsync(() => {
                ChartPoints = points;
                PrevPrice = _prevPrice;
                Price = last;
                CurrencySymbol = GetCurrencySymbol(currency);
                RangeLabel = $"{SelectedTimeRange.ToLocalizedString()} • {SelectedTimeInterval.ToLocalizedString()}";
                UpdatedDisplay = $"Updated: {DateTime.Now:hh:mm tt}";
            });
        } catch (Exception ex) {
            Debug.WriteLine($"[DEBUG] Yahoo fetch error: {ex.Message}");
        } finally {
            _isFetching = false;
        }
    }

    private void OnChanged(string name) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Interop with Win32 API")]
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);

    private void SendToBack() {
        var handle = new WindowInteropHelper(this).Handle;
        // no resize, no move, no change focus, ensure visible
        SetWindowPos(handle, HWND_BOTTOM, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010 | 0x0040);
    }

    public event Action<string>? RemoveTickerRequested;
    public event Action<string>? HideTickerRequested;

    private void AddTicker_Click(object sender, RoutedEventArgs e) {
        if (Topmost) {
            Topmost = false;
        }

        if (Application.Current is App app) {
            app.ShowMainWindow();
        }
    }

    private void RemoveTicker_Click(object sender, RoutedEventArgs e) {
        // Notify host and/or close this widget
        RemoveTickerRequested?.Invoke(Symbol);
        Close();
    }

    private void HideTicker_Click(object sender, RoutedEventArgs e) {
        // Soft-hide window; let host track hidden state if needed
        HideTickerRequested?.Invoke(Symbol);
        Hide();
    }

    private void PinMenuItem_Click(object sender, RoutedEventArgs e) {
        ToggleAlwaysOnTop_Click(sender, e);
    }

    private void CloseWidget_Click(object sender, RoutedEventArgs e) {
        Close();
    }
}