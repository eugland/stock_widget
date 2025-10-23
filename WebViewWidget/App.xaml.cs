using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using WebViewWidget.Properties;
using Application = System.Windows.Application;

namespace WebViewWidget;

public partial class App : Application {
    private DashboardWindow? _main;
    private bool _reallyExit;
    private NotifyIcon? _tray;

    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);
        var settings = SettingsService.Instance;


        // Set language
        var lang = settings.Language;
        var culture = new CultureInfo(lang);

        Thread.CurrentThread.CurrentCulture = culture; // dates/numbers
        Thread.CurrentThread.CurrentUICulture = culture; // RESX lookup

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));


        // 1) tray icon
        _tray = new NotifyIcon {
            Text = "Stock Widget",
            Icon = LoadIconFromResource("Assets/favicon.ico"),
            Visible = true
        };
        _tray.DoubleClick += (_, __) => ShowMainWindow();

        var menu = new ContextMenuStrip();
        menu.Items.Add("📊 Open Dashboard", null, (_, __) => ShowMainWindow());
        var symbols = (settings.PortfolioSymbols?.Select(q => q.Symbol) ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var showService = StockWindowService.Instance;
        var route =
            e.Args.FirstOrDefault(a => a.StartsWith("--route=", StringComparison.OrdinalIgnoreCase))?[
                "--route=".Length..] ?? "default";
        if (route.Equals("settings", StringComparison.OrdinalIgnoreCase)) {
            Debug.WriteLine("Recognized settings route");
            ShowMainWindow(DashboardPageIndex.Settings);
        }
        else {
            ShowMainWindow();
        }

        showService.ShowAllStockWindows();
        menu.Items.Add(Strings.Menu_ShowAllWidgets, null, (_, __) => showService.ShowAllStockWindows());
        menu.Items.Add(Strings.Menu_HideAllWidgets, null, (_, __) => showService.HideAllStockWindows());
        menu.Items.Add(Strings.Menu_Exits, null, (_, __) => ExitApp());
        _tray.ContextMenuStrip = menu;
    }

    private static Icon LoadIconFromResource(string resourcePath) {
        var uri = new Uri($"pack://application:,,,/{resourcePath}", UriKind.Absolute);
        using var stream = GetResourceStream(uri)!.Stream;
        return new Icon(stream);
    }

    public void ShowMainWindow(DashboardPageIndex index = DashboardPageIndex.Portfolio) {
        if (_main == null) {
            _main = new DashboardWindow(index);
            _main.Closing += Main_Closing; // safe to add here as well
        }
        else {
            _main.SelectTab(index);
        }

        _main.Show();
        if (_main.WindowState == WindowState.Minimized)
            _main.WindowState = WindowState.Normal;

        // to the top
        _main.Activate();
        _main.Topmost = true;
        _main.Topmost = false;
        _main.Focus();
    }

    private void ExitApp() {
        _reallyExit = true;
        _tray!.Visible = false;
        _tray.Dispose();
        _main?.Close();
        Shutdown();
    }

    private void Main_Closing(object? sender, CancelEventArgs e) {
        if (_reallyExit) return;
        e.Cancel = true;
        _main!.Hide();
        _tray?.ShowBalloonTip(1500, Strings.Tray_Header,
            Strings.Tray_ReopenHint,
            ToolTipIcon.Info);
    }
}