
using System.Diagnostics;
using System.Windows;
using Application = System.Windows.Application;

namespace WebViewWidget; // <- match your app's root namespace

public static class ToastService
{
    private static DashboardWindow? _dashboard;

    public static void Register(DashboardWindow window) => _dashboard = window;

    public static void Show(string message, int ms = 2500)
    {
        System.Diagnostics.Debug.WriteLine($"[ToastService] Showing toast: \"{message}\" (Duration={ms}ms)");

        // Be tolerant if window isn't ready yet
        if (_dashboard is not null) _dashboard.ShowToast(message, ms);
        else Application.Current?.Dispatcher?.Invoke(() =>
        {
            var win = Application.Current.Windows.OfType<DashboardWindow>().FirstOrDefault();
            win?.ShowToast(message, ms);
        });
    }
}