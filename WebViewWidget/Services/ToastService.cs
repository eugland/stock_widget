using System.Diagnostics;
using Application = System.Windows.Application;

namespace WebViewWidget;

public static class ToastService {
    private static DashboardWindow? _dashboard;

    public static void Register(DashboardWindow window) {
        _dashboard = window;
    }

    public static void Show(string message, int ms = 2500) {
        Debug.WriteLine($"[ToastService] Showing toast: \"{message}\" (Duration={ms}ms)");
        if (_dashboard is not null) _dashboard.ShowToast(message, ms);
        else
            Application.Current?.Dispatcher?.Invoke(() => {
                var win = Application.Current.Windows.OfType<DashboardWindow>().FirstOrDefault();
                win?.ShowToast(message, ms);
            });
    }
}