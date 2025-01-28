using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HybridApp.Manager
{
    public sealed partial class InstalledSitesPage : Page
    {
        private List<Site> sites;

        public InstalledSitesPage()
        {
            this.InitializeComponent();

            sites = ((App)Application.Current).MainSettingsManager.GetSitesList();
            if (sites is not null)
            {
                InstalledSites.ItemsSource = sites;

                if (sites.Count() == 0) 
                {
                    InstalledSites.Visibility = Visibility.Collapsed;
                    NoResultsText.Text = $"No installed sites";
                    NoResultsText.Visibility = Visibility.Visible;
                    AddNewSite.Visibility = Visibility.Visible;
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sites.Count() == 0)
                return;

            string query = SearchBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(query))
            {
                InstalledSites.ItemsSource = sites;
                InstalledSites.Visibility = Visibility.Visible;
                NoResultsText.Visibility = Visibility.Collapsed;
                return;
            }

            var filteredSites = sites
                .Select(site => new
                {
                    Site = site,
                    MatchScore = GetMatchScore(site.Name.ToLower(), query)
                })
                .Where(x => x.MatchScore != 0)
                .OrderBy(x => x.MatchScore)
                .Select(x => x.Site)
                .ToList();

            InstalledSites.ItemsSource = filteredSites;

            if (filteredSites.Count == 0) 
            {
                InstalledSites.Visibility = Visibility.Collapsed;
                NoResultsText.Text = $"No results for '{query}'";
                NoResultsText.Visibility = Visibility.Visible;
            }
            else
            {
                InstalledSites.Visibility = Visibility.Visible;
                NoResultsText.Visibility = Visibility.Collapsed;
            }
        }

        private int GetMatchScore(string source, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return 0;

            string[] queryTokens = query.Split(' ');

            int matchCount = 0;
            foreach (string t in queryTokens) 
            {
                if (source.Contains(t)) 
                {
                    matchCount++;
                }
            }

            return -matchCount;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var site = button?.DataContext as Site;

            if (site is null)
                return;

            SiteConfiguration sc = ((App)Application.Current).MainSettingsManager.GetSiteConfig(site.ID);
            ((MainWindow)((App)Application.Current).m_window).ContentNavigateToSettingsPage(sc);
        }

        private void AddNewSite_Click(object sender, RoutedEventArgs e)
        {
            ((App)(Application.Current)).SetMainNavViewIndex(3);
        }
    }
}
