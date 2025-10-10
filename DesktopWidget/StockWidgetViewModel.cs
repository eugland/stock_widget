using System.Windows.Threading;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;
using OoplesFinance.YahooFinanceAPI.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace DesktopWidget;

public class StockWidgetViewModel : IDisposable
{
    private readonly string _symbol;
    private readonly YahooClient _yahooClient = new();
    private readonly DispatcherTimer _timer;
    private readonly CancellationTokenSource _cts = new();

    public string Title { get; private set; }
    public PlotModel PlotModel { get; }

    public StockWidgetViewModel(string symbol)
    {
        _symbol = symbol;
        Title = $"{symbol} — 1D (2m)";

        PlotModel = new PlotModel
        {
            PlotAreaBorderColor = OxyColors.Gray,
            TextColor = OxyColors.White
        };

        // X-axis = time labels, Y-axis = price
        var x = new CategoryAxis
        {
            Position = AxisPosition.Bottom,
            IsZoomEnabled = false,
            IsPanEnabled = false,
            GapWidth = 0.5
        };

        var y = new LinearAxis
        {
            Position = AxisPosition.Right,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.None,
            IsZoomEnabled = false,
            IsPanEnabled = false
        };

        PlotModel.Axes.Add(x);
        PlotModel.Axes.Add(y);

        PlotModel.Series.Add(new CandleStickSeries
        {
            IncreasingColor = OxyColors.ForestGreen,
            DecreasingColor = OxyColors.IndianRed,
            CandleWidth = 0.5,
            TrackerFormatString = "{0}\n{1}\nO:{2:0.###} H:{3:0.###}\nL:{4:0.###} C:{5:0.###}"
        });

        PlotModel.Series.Add(new LineSeries
        {
            StrokeThickness = 1.2,
            LineStyle = LineStyle.Solid,
            Color = OxyColors.Gold
        });

        // Set up refresh timer
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        _timer.Tick += async (_, __) => await RefreshAsync();
        _ = RefreshAsync();  // initial load
        _timer.Start();
    }

    public async Task RefreshAsync()
    {
        try
        {
            var ci = await _yahooClient.GetChartInfoAsync(_symbol, TimeRange._1Day, TimeInterval._2Minutes);

            var xAxis = (CategoryAxis)PlotModel.Axes[0];
            var candleSeries = (CandleStickSeries)PlotModel.Series[0];
            var lineSeries = (LineSeries)PlotModel.Series[1];

            xAxis.ItemsSource = ci.DateList.Select(d => d.ToLocalTime().ToString("HH:mm")).ToList();
            candleSeries.Items.Clear();
            lineSeries.Points.Clear();

            for (int i = 0; i < ci.DateList.Count; i++)
            {
                candleSeries.Items.Add(new HighLowItem(
                    x: i,
                    high: ci.HighList[i],
                    low: ci.LowList[i],
                    open: ci.OpenList[i],
                    close: ci.CloseList[i]));
                lineSeries.Points.Add(new DataPoint(i, ci.CloseList[i]));
            }

            PlotModel.InvalidatePlot(true);
        }
        catch (Exception ex)
        {
            // Optional: log or show a status message
            Console.WriteLine($"[Widget] Refresh failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _cts.Cancel();
        _cts.Dispose();
    }
}