using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
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
using Microsoft.UI.Xaml.Shapes;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HybridApp.Manager
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        private List<Site> sites = new List<Site>();

        public HomePage()
        {
            this.InitializeComponent();
            this.ActualThemeChanged += HomePage_ActualThemeChanged;
            this.Loaded += HomePage_Loaded;

            sites = ((App)Application.Current).MainSettingsManager.GetSitesList();
            if (sites is not null)
            {
                if (sites.Count() == 0)
                {
                    InstalledSites.Visibility = Visibility.Collapsed;
                    NoResultsText.Text = $"No installed sites";
                    NoResultsText.Visibility = Visibility.Visible;
                    ManageInstalledSites.Visibility = Visibility.Visible;
                }
                else 
                {
                    List<Site> newSites = new List<Site>();

                    foreach (Site s in sites) 
                    {
                        if (s.QuickAccess)
                        {
                            newSites.Add(s);
                        }
                    }

                    if (newSites.Count <= 0) 
                    {
                        InstalledSites.Visibility = Visibility.Collapsed;
                        NoResultsText.Text = $"Quick access is empty";
                        NoResultsText.Visibility = Visibility.Visible;
                        ManageQuickAccess.Visibility = Visibility.Visible;
                    }

                    sites = newSites;
                    InstalledSites.ItemsSource = sites;
                }

            }
        }

        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateWindowTheme(this.ActualTheme);
        }

        private void UpdateWindowTheme(ElementTheme theme) 
        {
            if (theme == ElementTheme.Light)
            {
                ImageBackdrop.Source = InfoHelper.CreateBitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Assets\HybridAppHomePageLight.png")));
            }
            else if (theme == ElementTheme.Dark)
            {
                ImageBackdrop.Source = InfoHelper.CreateBitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Assets\HybridAppHomePageDark.png")));
            }
            else if (theme == ElementTheme.Default)
            {
                ImageBackdrop.Source = InfoHelper.CreateBitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Assets\HybridAppHomePageDark.png")));
            }
        }

        private void HomePage_ActualThemeChanged(FrameworkElement sender, object args)
        {
            UpdateWindowTheme(sender.ActualTheme);
        }

        private void ManageQuickAccess_Click(object sender, RoutedEventArgs e)
        {
            ((App)(Application.Current)).SetMainNavViewIndex(1);
        }

        private void ManageInstalledSites_Click(object sender, RoutedEventArgs e)
        {
            ((App)(Application.Current)).SetMainNavViewIndex(2);
        }

        private void AddNewSiteButton_Click(object sender, RoutedEventArgs e)
        {
            ((App)(Application.Current)).SetMainNavViewIndex(3);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ((App)(Application.Current)).SetMainNavViewIndexFooter(0);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void SiteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var site = button?.DataContext as Site;

            if (site is null)
                return;

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = System.IO.Path.Combine(((App)Application.Current).MainSettingsManager.HOST_PATH, "HybridApp.Host.exe");
            psi.Arguments = site.ID;
            psi.UseShellExecute = false;
            Process.Start(psi);
        }
    }
}
