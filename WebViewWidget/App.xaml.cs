using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.Logging;
using WebViewWidget.service;

namespace WebViewWidget;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Window win;

        // Logic: decide which window to open
        if (e.Args.Contains("--native"))
            win = new NativeWindow();
        else
            win = new MainWindow();

        MainWindow = win;
        win.Show();
    }
}