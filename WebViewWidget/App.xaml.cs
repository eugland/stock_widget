
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using WebViewWidget.Properties;
using WebViewWidget.Utils;
using Application = System.Windows.Application;

namespace WebViewWidget;

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
        var settings = SettingsService.Instance;

        string route = e.Args.FirstOrDefault(a => a.StartsWith("--route=", StringComparison.OrdinalIgnoreCase))
                      ?.Substring("--route=".Length) ?? "default";

        // Get normalized 2-letter language from your SettingsService
        var lang = settings.Language; // e.g., "en", "zh", "ja", "ko", "es"
        var culture = new CultureInfo(lang);

        Thread.CurrentThread.CurrentCulture = culture;      // dates/numbers
        Thread.CurrentThread.CurrentUICulture = culture;    // RESX lookup

        // Make WPF element language follow Culture (affects number/date formatting in bindings)
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));


        // 1) Build tray icon FIRST
        _tray = new NotifyIcon
        {
            Text = Strings.Tray_Header,
            Icon = LoadIconFromResource("Assets/favicon.ico"),
            Visible = true
        };
        _tray.DoubleClick += (_, __) => ShowMainWindow();

        var menu = new ContextMenuStrip();

        menu.Items.Add(Strings.Menu_OpenDashboard, Tools.LoadEmbeddedImage("speedometer.png"), (_, __) => ShowMainWindow());

        // optional: quick submenu to open ticker windows on demand
        var symbols = (settings.PortfolioSymbols?.Select(q => q.Symbol)
                       ?? Enumerable.Empty<string>())
                      .Where(s => !string.IsNullOrWhiteSpace(s))
                      .Distinct(StringComparer.OrdinalIgnoreCase)
                      .ToList();

        var showService = StockWindowService.Instance;
        if (route.Equals("settings", StringComparison.OrdinalIgnoreCase))
        {
            Debug.WriteLine("Recognized settings route");
            ShowMainWindow(1);
        } else
        {
            ShowMainWindow(0);
        }
        showService.ShowAllStockWindows();
        menu.Items.Add(Strings.Menu_ShowAllWidgets, Tools.LoadEmbeddedImage("widgets.png"), (_, __) => showService.ShowAllStockWindows());
        menu.Items.Add(Strings.Menu_HideAllWidgets, Tools.LoadEmbeddedImage("hidden.png"), (_, __) => showService.HideAllStockWindows()); 
        menu.Items.Add(Strings.Menu_Exits, Tools.LoadEmbeddedImage("delete.png"), (_, __) => ExitApp());
        _tray.ContextMenuStrip = menu;
    }
    

    private static Icon LoadIconFromResource(string resourcePath)
    {
        var uri = new Uri($"pack://application:,,,/{resourcePath}", UriKind.Absolute);
        using var stream = Application.GetResourceStream(uri)!.Stream;
        return new Icon(stream);
    }

    private void ShowMainWindow(int index = 0)
    {
        if (_main == null)
        {
            _main = new DashboardWindow(index);
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
        _tray?.ShowBalloonTip(1500, Strings.Tray_Header,
            Strings.Tray_ReopenHint,
            ToolTipIcon.Info);
    }

}