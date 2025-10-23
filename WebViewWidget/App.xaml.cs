using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using WebViewWidget.Properties;
using WebViewWidget.Utils;
using Application = System.Windows.Application;

namespace WebViewWidget;

public partial class App : Application {
    private DashboardWindow? _main;
    private bool _reallyExit;
    private NotifyIcon? _tray;

    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);
        var settings = SettingsService.SettingsServ;

        var lang = settings.Language;
        var culture = new CultureInfo(lang);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        _tray = new NotifyIcon {
            Text = Strings.Tray_Header,
            Icon = LoadIconFromResource("Assets/favicon.ico"),
            Visible = true
        };
        _tray.DoubleClick += (_, _) => ShowMainWindow();

        var menu = new ContextMenuStrip();
        menu.Items.Add(Strings.Menu_OpenDashboard, Tools.LoadEmbeddedImage("speedometer.png"),
            (_, _) => ShowMainWindow());
        var showService = StockWindowService.Instance;
        var route =
            e.Args.FirstOrDefault(a => a.StartsWith("--route=", StringComparison.OrdinalIgnoreCase))?[
                "--route=".Length..] ?? "default";
        if (route.Equals("settings", StringComparison.OrdinalIgnoreCase)) {
            Debug.WriteLine("Recognized settings route");
            ShowMainWindow(DashboardPageIndex.Settings);
        } else {
            ShowMainWindow();
        }
        showService.ShowAllStockWindows();
        menu.Items.Add(Strings.Menu_ShowAllWidgets, Tools.LoadEmbeddedImage("widgets.png"),
            (_, _) => showService.ShowAllStockWindows());
        menu.Items.Add(Strings.Menu_HideAllWidgets, Tools.LoadEmbeddedImage("hidden.png"),
            (_, _) => showService.HideAllStockWindows());
        menu.Items.Add(Strings.Menu_Exits, Tools.LoadEmbeddedImage("delete.png"), (_, __) => ExitApp());
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
        } else {
            _main.SelectTab(index);
        }

        _main.Show();
        if (_main.WindowState == WindowState.Minimized) {
            _main.WindowState = WindowState.Normal;
        }
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
        if (_reallyExit) {
            return;
        }
        e.Cancel = true;
        _main!.Hide();
        _tray?.ShowBalloonTip(1500, Strings.Tray_Header,
            Strings.Tray_ReopenHint,
            ToolTipIcon.Info);
    }
}