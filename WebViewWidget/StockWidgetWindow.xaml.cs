// WebViewWidget.NativeWindow/MainWindow.xaml.cs

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using System.Windows;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;
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

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Symbol {
            get => _symbol;
            set {
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

        public StockWidgetWindow(string ticker) {
            Symbol = ticker;
            InitializeComponent();
            DataContext = this;

            // Initial load & periodic refresh from Yahoo
            _ = RefreshFromYahooAsync();
            _timer.Elapsed += async (_, _) => await RefreshFromYahooAsync();
            _timer.AutoReset = true;
            _timer.Start();
        }

        private async Task RefreshFromYahooAsync() {
            if (_isFetching) return;
            _isFetching = true;

            try {
                var ci = await _yahoo.GetChartInfoAsync(Symbol, TimeRange._1Day, TimeInterval._2Minutes);

                if (ci.DateList == null || ci.DateList.Count == 0 ||
                    ci.CloseList == null || ci.CloseList.Count == 0)
                    return;

                var points = ci.CloseList
                    .Select((p, i) => new Point(i, p))
                    .ToList();

                var first = points.First().Y;
                var last = points.Last().Y;

                await Dispatcher.InvokeAsync(() => {
                    ChartPoints = points;
                    PrevPrice = first;
                    Price = last;
                    RangeLabel = "1D • 2m";
                    UpdatedDisplay = $"Updated: {DateTime.Now:hh:mm tt}";
                });
            }
            catch (Exception ex) {
                Console.WriteLine($"[DEBUG] Yahoo fetch error: {ex.Message}");
            }
            finally {
                _isFetching = false;
            }
        }

        private void OnChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        
    }
}