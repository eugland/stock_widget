// WebViewWidget.NativeWindow/MainWindow.xaml.cs

using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Timer = System.Timers.Timer;

namespace WebViewWidget {
    public partial class StockWidgetWindow : Window, INotifyPropertyChanged {
        private readonly Timer _timer = new(60_000); // refresh every 60s
        private readonly YahooClient _yahoo = new();
        private bool _isFetching = false;

        private double _price;
        private double _prevPrice;
        private string _symbol = "CRWV";
        private string _rangeLabel = "1D • 2m";
        private string _updatedDisplay = "";
        private List<Point> _chartPoints = new();
        private Button? _activeButton;

        public event PropertyChangedEventHandler? PropertyChanged;

        public TimeRange SelectedTimeRange { get; private set; } = TimeRange._1Day;
        public TimeInterval SelectedTimeInterval { get; private set; } = TimeInterval._2Minutes;

        public void ChangeTimeRange(TimeRange range, TimeInterval interval)
        {
            SelectedTimeRange = range;
            SelectedTimeInterval = interval;

            _ = RefreshFromYahooAsync();
        }

        private void setSelectedDateButton(Button button)
        {
            // Reset previous button style
            if (_activeButton != null)
            {
                _activeButton.FontWeight = FontWeights.Regular;
                _activeButton.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(169, 177, 187)); // default gray
                _activeButton.Background = Brushes.Transparent;
            }

            button.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(86, 182, 247));
            button.FontWeight = FontWeights.Bold;
            button.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 86, 182, 247));

            _activeButton = button;
        }

        private void TimeRangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string tag) return;


            var parts = tag.Split(',');
            if (parts.Length != 2) return;

            if (Enum.TryParse<TimeRange>(parts[0], out var range) &&
                Enum.TryParse<TimeInterval>(parts[1], out var interval))
            {
                Debug.WriteLine($"[DEBUG] Parsed TimeRange={range}, TimeInterval={interval}. Invoking ChangeTimeRange.");
                ChangeTimeRange(range, interval);

                if (_activeButton != null)
                    _activeButton.Foreground = new SolidColorBrush(Color.FromRgb(169, 177, 187)); // #A9B1BB

                setSelectedDateButton(button);
            }
            Debug.WriteLine("[DEBUG] Exiting TimeRangeButton_Click.");
        }


        public string Symbol {
            get => _symbol;
            set {
                _symbol = value;
                OnChanged(nameof(Symbol));
            }
        }
        private void ToggleAlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            if (Topmost)
            {
                Topmost = false;
                SendToBack();
            }
            else
            {
                Topmost = true;
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
                double change = Price - PrevPrice;
                double pct = PrevPrice != 0 ? change / PrevPrice * 100 : 0;
                string sign = change >= 0 ? "+" : "";
                return $"{sign}{change:F2} ({sign}{pct:F2}%)";
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

        private static bool IsOnInteractiveElement(DependencyObject? d)
        {
            while (d != null)
            {
                d = VisualTreeHelper.GetParent(d);
            }
            return false;
        }

        public StockWidgetWindow(string ticker) {
            Symbol = ticker;
            InitializeComponent();
            DataContext = this;

            RootGrid.MouseLeftButtonDown += (_, e) =>
            {
                // Don't drag when clicking interactive controls (buttons, inputs, etc.)
                if (IsOnInteractiveElement(e.OriginalSource as DependencyObject)) return;
                try { DragMove(); } catch { /* ignore if drag starts during resize */ }
            };


            Loaded += (_, __) => SendToBack();
            _ = RefreshFromYahooAsync();
            _timer.Elapsed += async (_, _) => await RefreshFromYahooAsync();
            _timer.AutoReset = true;
            _timer.Start();
            setSelectedDateButton(Btn1D);
            Debug.WriteLine("starting " + ticker);
        }



        private async Task RefreshFromYahooAsync() {
            if (_isFetching) return;
            _isFetching = true;

            try {
                var ci = await _yahoo.GetChartInfoAsync(Symbol, SelectedTimeRange, SelectedTimeInterval);

                if (ci.DateList == null || ci.DateList.Count == 0 ||
                    ci.CloseList == null || ci.CloseList.Count == 0)
                    return;

                var startTime = DateTime.Now;
                var points = ci.CloseList
                    .Select((p, i) => new Point(i, p))
                    .ToList();
                var endTime = DateTime.Now;
                Debug.WriteLine($"[DEBUG] Total points: {points.Count}, Duration: {(endTime - startTime).TotalMilliseconds} ms");

                var first = points.First().Y;
                var last = points.Last().Y;

                await Dispatcher.InvokeAsync(() => {
                    ChartPoints = points;
                    PrevPrice = first;
                    Price = last;
                    RangeLabel = $"{SelectedTimeRange.ToString().Replace("_", "")} • {SelectedTimeInterval.ToString().Replace("_", "")}";
                    UpdatedDisplay = $"Updated: {DateTime.Now:hh:mm tt}";
                });
            }
            catch (Exception ex) {
                Debug.WriteLine($"[DEBUG] Yahoo fetch error: {ex.Message}");
            }
            finally {
                _isFetching = false;
            }
        }

        private void OnChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);


        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOACTIVATE = 0x0010;
        private const int SWP_SHOWWINDOW = 0x0040;

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        private void SendToBack()
        {
            var handle = new WindowInteropHelper(this).Handle;
            SetWindowPos(handle, HWND_BOTTOM, 0, 0, 0, 0,
                SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

    }
}