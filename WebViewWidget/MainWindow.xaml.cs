using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;
using Timer = System.Timers.Timer;

namespace WebViewWidget;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private readonly YahooClient _yahoo = new();
    private readonly Timer _timer = new(300_000); // 5 minutes
    private string _symbol = "CRWV";

    public MainWindow() {
        InitializeComponent();
       
    }


    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
        Console.WriteLine("MouseLeftButtonDown at ");
        DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) {
        Close();
    }


    private async Task FetchAndPushAsync() {
        // 🔍 Debug message at entry
        Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff} → FetchAndPushAsync() called for symbol: {_symbol}");

        var ci = await _yahoo.GetChartInfoAsync(_symbol, TimeRange._1Day, TimeInterval._2Minutes);
        if (ci.DateList.Count == 0) {
            Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff} → No data returned for {_symbol}");
            return;
        }

        double price = ci.CloseList[^1];
        double prev = ci.CloseList.FirstOrDefault();
        double change = price - prev;
        double changePct = prev != 0 ? (change / prev) * 100.0 : 0.0;

        var payload = new {
            symbol = _symbol,
            price,
            change,
            changePct,
            timestamps = ci.DateList
                .Select(dt => new DateTimeOffset(dt).ToUnixTimeSeconds())
                .ToList(),
            prices = ci.CloseList
        };

        var jsonContent = JsonSerializer.Serialize(payload);
        Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff} → JSON payload generated for {_symbol}");
        Console.WriteLine(jsonContent);

        Web.CoreWebView2.PostWebMessageAsJson(jsonContent);

        Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff} → Data pushed to WebView2 for {_symbol}");
    }

    private async void Window_Loaded(object? sender, RoutedEventArgs e) {
        await Web.EnsureCoreWebView2Async();
        Web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        Web.CoreWebView2.Settings.IsZoomControlEnabled = false;
        Web.CoreWebView2.Settings.IsStatusBarEnabled = false;

        // Write HTML to a temp file and navigate
        var html = BuildHtml();
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "stock_widget.html");
        await System.IO.File.WriteAllTextAsync(path, html, Encoding.UTF8);
        Web.Source = new Uri(path);

        await FetchAndPushAsync();
        _timer.Start();
    }

    private static string BuildHtml() =>
        """
        <!doctype html>
        <html>
        <head>
        <meta charset="utf-8"/>
        <meta http-equiv="X-UA-Compatible" content="IE=edge" />
        <meta name="viewport" content="width=device-width,initial-scale=1"/>
        <script src="https://cdn.tailwindcss.com"></script>
        <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
        <style>
          :root { color-scheme: dark; }
          html, body { height: 100%; }
          body { margin: 0; background: transparent; font-family: ui-sans-serif, system-ui, -apple-system; }
          .glass {
            background: rgba(17, 17, 17, 0.55);
            backdrop-filter: blur(16px) saturate(120%);
            -webkit-backdrop-filter: blur(16px) saturate(120%);
            border-radius: 18px;
            border: 1px solid rgba(255,255,255,0.08);
            box-shadow: 0 10px 30px rgba(0,0,0,0.35);
          }
          .chip { background: rgba(255,255,255,0.08); border: 1px solid rgba(255,255,255,0.06); }
          .up   { color: #8fff9f; }
          .down { color: #ff8f8f; }
          * { user-select: none; -webkit-user-select: none; }
          /* Make the canvas fill its container */
          #spark { width: 100% !important; height: 100% !important; display: block; }
        </style>
        </head>
        <body>
          <!-- Fill all available space -->
          <div id="root" class="w-full h-full p-3">
            <!-- Card stretches to full size -->
            <div class="glass w-full h-full p-4 flex flex-col gap-3">
              <!-- Header -->
              <div class="flex items-center justify-between">
                <div class="flex items-baseline gap-2">
                  <div id="sym" class="text-xl font-semibold tracking-wide">—</div>
                  <div id="range" class="text-xs opacity-70">1D • 2m</div>
                </div>
                <div class="flex items-baseline gap-2">
                  <div id="price" class="text-2xl font-bold">—</div>
                  <div id="delta" class="text-sm chip px-2 py-1 rounded-full">—</div>
                </div>
              </div>

              <!-- Chart area grows/shrinks with the card -->
              <div id="chartWrap" class="flex-1 min-h-[80px]">
                <canvas id="spark"></canvas>
              </div>

              <!-- Footer -->
              <div class="flex justify-between text-xs opacity-70">
                <div>Updated: <span id="time">—</span></div>
              </div>
            </div>
          </div>

        <script>
          let chart;
          const ctx = document.getElementById('spark').getContext('2d');

          function fmtTime(ts) {
            try { return new Date(ts*1000).toLocaleTimeString([], {hour: '2-digit', minute: '2-digit'}); }
            catch { return '—'; }
          }
          function fmtPrice(v) { return (v ?? 0).toLocaleString(undefined, {maximumFractionDigits: 4}); }
          function sign(v) { return v >= 0 ? '+' : '−'; }

          function ensureChart(labels, data, isUp) {
            if (chart) {
              chart.data.labels = labels;
              chart.data.datasets[0].data = data;
              chart.data.datasets[0].borderColor = isUp ? 'rgba(0, 255, 128, 0.9)' : 'rgba(255, 80, 80, 0.9)';
              chart.data.datasets[0].backgroundColor = isUp ? 'rgba(0, 255, 128, 0.12)' : 'rgba(255, 80, 80, 0.12)';
              chart.update();
              return;
            }
            chart = new Chart(ctx, {
              type: 'line',
              data: { labels, datasets: [{ data, fill: true, tension: 0.3, pointRadius: 0, borderWidth: 2 }] },
              options: {
                plugins: { legend: { display: false }, tooltip: { enabled: true } },
                scales: { x: { display: false }, y: { display: false } },
                animation: { duration: 200 },
                responsive: true,
                maintainAspectRatio: false   // <-- critical for container-driven height
              }
            });

            // Ensure Chart.js resizes when the container changes (WebView2 resizes)
            const wrap = document.getElementById('chartWrap');
            if (window.ResizeObserver && wrap) {
              const ro = new ResizeObserver(() => { chart && chart.resize(); });
              ro.observe(wrap);
            } else {
              // Fallback: on window resize
              window.addEventListener('resize', () => chart && chart.resize());
            }
          }

          function updateUI(payload) {
            const { symbol, price, change, changePct, timestamps, prices } = payload;
            const isUp = (change ?? 0) >= 0;

            document.getElementById('sym').textContent = symbol ?? '—';
            document.getElementById('price').textContent = fmtPrice(price);

            const deltaEl = document.getElementById('delta');
            const s = sign(change ?? 0);
            deltaEl.textContent = `${s}${Math.abs(change ?? 0).toFixed(2)} (${s}${Math.abs(changePct ?? 0).toFixed(2)}%)`;
            deltaEl.classList.toggle('up', isUp);
            deltaEl.classList.toggle('down', !isUp);

            const labels = (timestamps ?? []).map(fmtTime);
            ensureChart(labels, prices ?? [], isUp);

            const lastTs = timestamps?.length ? timestamps[timestamps.length-1] : null;
            document.getElementById('time').textContent = lastTs ? fmtTime(lastTs) : new Date().toLocaleTimeString();
          }

          // receive push from .NET
          window.chrome?.webview?.addEventListener('message', e => {
            try { updateUI(e.data); } catch {}
          });
        </script>
        </body>
        </html>
        """;
}