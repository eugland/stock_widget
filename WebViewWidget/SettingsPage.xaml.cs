using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace WebViewWidget;

public partial class SettingsPage : Page
{
    private static readonly SettingsService settings = SettingsService.Instance;

    public SettingsPage()
    {
        InitializeComponent();
        DataContext = settings;
        Loaded += (_, __) =>
        {
            LanguageCombo.SelectedValue = settings.Language;
        };
    }



    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var sel = LanguageCombo.SelectedValue as string;
        if (!string.IsNullOrWhiteSpace(sel) &&
            !string.Equals(sel, settings.Language, System.StringComparison.OrdinalIgnoreCase))
        {
            settings.Language = sel;
        }
    }

    private void OpenMicrosoftStore_Click(object sender, RoutedEventArgs e)
    {
        const string storeUri = "ms-windows-store://pdp/?productid=9WZDNCRFJ2R6";
        Process.Start(new ProcessStartInfo(storeUri) { UseShellExecute = true });
    }

}
