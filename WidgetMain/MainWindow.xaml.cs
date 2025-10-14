using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using WidgetMain.Pages;
using WidgetMain.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WidgetMain
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainWindow_Loaded();
        }

        private async void MainWindow_Loaded()
        {
            // 1) Load settings from LocalState/settings.json (if present)
            await SettingsService.Instance.InitializeAsync();

            // 2) Apply them to the window/shell
            ApplySettings();

            // 3) Show Settings page (default for now)
            RootFrame.Navigate(typeof(SettingsPage));
            Nav.SelectedItem = Nav.MenuItems
                .OfType<NavigationViewItem>()
                .First(mi => (string)mi.Tag == "Settings");
        }

        private void ApplySettings()
        {
            var s = SettingsService.Instance.Settings;

            // Theme
            if (Content is FrameworkElement fe)
            {
                fe.RequestedTheme = s.Theme switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }

            // Mica
            SystemBackdrop = s.UseMica ? new Microsoft.UI.Xaml.Media.MicaBackdrop() : null;

            // Window opacity
            //this.Opacity = s.Opacity;

            // Always on top
            // WindowInterop.SetAlwaysOnTop(this, s.AlwaysOnTop);
        }

        private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();
            switch (tag)
            {
                case "Settings":
                    if (RootFrame.Content?.GetType() != typeof(SettingsPage))
                        RootFrame.Navigate(typeof(SettingsPage));
                    break;

                case "Dashboard":
                    // TODO: navigate to your dashboard/home page when you have one.
                    // For now, we can keep showing Settings:
                    if (RootFrame.Content?.GetType() != typeof(SettingsPage))
                        RootFrame.Navigate(typeof(SettingsPage));
                    break;
            }
        }
    }
}