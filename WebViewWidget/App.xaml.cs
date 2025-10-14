using Microsoft.Extensions.Logging;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using Application = System.Windows.Application;
using Forms = System.Windows.Forms;

namespace WebViewWidget
{
    public partial class App : Application
    {
        private NotifyIcon? _tray;
        private DashboardWindow? _main;
        private bool _reallyExit;

        public App()
        {
  
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1) Build tray icon FIRST
            _tray = new NotifyIcon
            {
                Text = "Stock Widget",
                Icon = LoadIconFromResource("Assets/favicon.ico"),
                Visible = true
            };
            _tray.DoubleClick += (_, __) => ShowMainWindow();

            var menu = new ContextMenuStrip();
            menu.Items.Add("Open Dashboard", null, (_, __) => ShowMainWindow());

            // optional: quick submenu to open ticker windows on demand
            var settings = SettingsService.Instance;
            var symbols = (settings.PortfolioSymbols?.Select(q => q.Symbol)
                           ?? Enumerable.Empty<string>())
                          .Where(s => !string.IsNullOrWhiteSpace(s))
                          .Distinct(StringComparer.OrdinalIgnoreCase)
                          .ToList();

            if (symbols.Count > 0)
            {
                var tickers = new ToolStripMenuItem("Open Ticker…");
                foreach (var sym in symbols)
                    tickers.DropDownItems.Add(sym, null, (_, __) => ShowStockWindow(sym));
                menu.Items.Add(tickers);
            }

            menu.Items.Add("Exit", null, (_, __) => ExitApp());
            _tray.ContextMenuStrip = menu;
        }

        private static Icon LoadIconFromResource(string resourcePath)
        {
            var uri = new Uri($"pack://application:,,,/{resourcePath}", UriKind.Absolute);
            using var stream = Application.GetResourceStream(uri)!.Stream;
            return new Icon(stream);
        }

        private void ShowMainWindow()
        {
            if (_main == null)
            {
                _main = new DashboardWindow();
            }
            _main.Show();
            _main.WindowState = WindowState.Normal;
            _main.Activate();
        }

        private void ExitApp()
        {
            _reallyExit = true;
            _tray!.Visible = false;
            _tray.Dispose();
            _main?.Close();
            Shutdown();
        }

        private void Main_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_reallyExit) return; // allow actual exit

            // Hide instead of close → keeps tray icon alive
            e.Cancel = true;
            _main!.Hide();
            _tray?.ShowBalloonTip(1500, "Stock Widget",
                "Still running here. Double-click to reopen.",
                ToolTipIcon.Info);
        }

        private void ShowStockWindow(string symbol)
        {
            var w = new StockWidgetWindow(symbol);
            w.Closing += (s, e) => { e.Cancel = true; ((Window)s!).Hide(); };
            w.Show();
            w.Activate();
        }
    }
}