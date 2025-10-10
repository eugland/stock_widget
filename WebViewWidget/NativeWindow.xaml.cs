using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


// WebViewWidget.NativeWindow/MainWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using System.Windows;
using Timer = System.Timers.Timer;

namespace WebViewWidget
{
    public partial class NativeWindow : Window, INotifyPropertyChanged
    {
        private readonly Timer _timer = new(10_000); // refresh every 10s (demo)
        private readonly Random _rng = new();

        private double _price;
        private double _prevPrice;
        private string _symbol = "CRWV";
        private string _rangeLabel = "1D • 2m";
        private string _updatedDisplay = "";
        private List<Point> _chartPoints = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Symbol { get => _symbol; set { _symbol = value; OnChanged(nameof(Symbol)); } }
        public string RangeLabel { get => _rangeLabel; set { _rangeLabel = value; OnChanged(nameof(RangeLabel)); } }

        public double Price { get => _price; set { _price = value; OnChanged(nameof(Price)); OnChanged(nameof(ChangeDisplay)); } }
        public double PrevPrice { get => _prevPrice; set { _prevPrice = value; OnChanged(nameof(ChangeDisplay)); } }

        public string ChangeDisplay
        {
            get
            {
                double change = Price - PrevPrice;
                double pct = PrevPrice != 0 ? change / PrevPrice * 100 : 0;
                string sign = change >= 0 ? "+" : "";
                string color = change >= 0 ? "#72D17C" : "#D17272";
                return $"{sign}{change:F2} ({sign}{pct:F2}%)";
            }
        }

        public string UpdatedDisplay
        {
            get => _updatedDisplay;
            set { _updatedDisplay = value; OnChanged(nameof(UpdatedDisplay)); }
        }

        public List<Point> ChartPoints
        {
            get => _chartPoints;
            set { _chartPoints = value; OnChanged(nameof(ChartPoints)); }
        }

        public NativeWindow()
        {
            InitializeComponent();
            DataContext = this;

            GenerateInitialChart();
            _timer.Elapsed += (_, _) => Dispatcher.Invoke(UpdateChart);
            _timer.Start();
        }

        private void GenerateInitialChart()
        {
            // Generate synthetic price data
            var now = DateTime.Now;
            var points = new List<Point>();
            double basePrice = 135;
            double price = basePrice;

            for (int i = 0; i < 60; i++)
            {
                price += _rng.NextDouble() * 2 - 1; // small random walk
                points.Add(new Point(i, price));
            }

            ChartPoints = points;
            PrevPrice = points.First().Y;
            Price = points.Last().Y;
            UpdatedDisplay = $"Updated: {now:hh:mm tt}";
        }

        private void UpdateChart()
        {
            // Shift chart with new price
            if (ChartPoints.Count == 0) return;

            PrevPrice = Price;
            double lastY = ChartPoints.Last().Y;
            double nextY = lastY + _rng.NextDouble() * 3 - 1.5;
            if (nextY < 0) nextY = 0;

            var newList = ChartPoints.Skip(1).ToList();
            newList.Add(new Point(ChartPoints.Last().X + 1, nextY));
            ChartPoints = newList;

            Price = nextY;
            UpdatedDisplay = $"Updated: {DateTime.Now:hh:mm tt}";
        }

        private void OnChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
