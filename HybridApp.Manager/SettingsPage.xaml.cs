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
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HybridApp.Manager
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            this.Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> options = ((App)Application.Current).MainSettingsManager.GetAdditionalOptions();
            if (options.Keys.Contains("ApplicationTheme"))
            {
                int theme = Convert.ToInt32(options["ApplicationTheme"]);
                LightDarkModeRadioButtons.SelectedIndex = theme;
            }
            else
            {
                LightDarkModeRadioButtons.SelectedIndex = 2;
            }
        }

        private void LightTheme_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).MainSettingsManager.AddOption("ApplicationTheme", "0");
            ((App)Application.Current).MainSettingsManager.WriteConfig();
            Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
        }

        private void DarkTheme_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).MainSettingsManager.AddOption("ApplicationTheme", "1");
            ((App)Application.Current).MainSettingsManager.WriteConfig();
            Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
        }

        private void SystemTheme_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).MainSettingsManager.AddOption("ApplicationTheme", "2");
            ((App)Application.Current).MainSettingsManager.WriteConfig();
            Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
        }
    }
}
