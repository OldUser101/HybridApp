using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public sealed partial class QuickAccessPage : Page
    {
        private List<Site> sites;
        private ObservableCollection<Site> quickAccess;
        private ObservableCollection<Site> available;

        public QuickAccessPage()
        {
            this.InitializeComponent();

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
                    quickAccess = new ObservableCollection<Site>();
                    available = new ObservableCollection<Site>();

                    QuickAccessSites.ItemsSource = quickAccess;
                    AvailableSites.ItemsSource = available;

                    foreach (Site s in sites) 
                    {
                        if (s.QuickAccess)
                        {
                            quickAccess.Add(s);
                        }
                        else 
                        {
                            available.Add(s);
                        }
                    }
                }
            }
        }

        private void ManageInstalledSites_Click(object sender, RoutedEventArgs e)
        {
            ((App)(Application.Current)).SetMainNavViewIndex(2);
        }

        private int IndexBySiteId(IEnumerable<Site> sites, Site site) 
        {
            for (int i = 0; i < sites.Count(); i++) 
            {
                if (sites.ElementAt(i).ID == site.ID) 
                {
                    return i;
                }
            }
            return -1;
        }

        private void SiteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var site = button?.DataContext as Site;

            if (site is null)
                return;

            int availableIndex = IndexBySiteId(available, site);
            int quickAccessIndex = IndexBySiteId(quickAccess, site);
            SiteConfiguration sc = ((App)Application.Current).MainSettingsManager.GetSiteConfig(site.ID);

            if (site.QuickAccess)
            {
                site.QuickAccess = false;
                quickAccess.RemoveAt(quickAccessIndex);
                available.Add(site);
                sc.QuickAccess = false;
            }
            else 
            {
                site.QuickAccess = true;
                quickAccess.Add(site);
                available.RemoveAt(availableIndex);
                sc.QuickAccess = true;
            }

            ((App)Application.Current).MainSettingsManager.ReConfigureSite(site.ID, sc, true);
            ((App)Application.Current).MainSettingsManager.SiteRootConfigUpdateQuickAccessState(site.ID, site.QuickAccess);
        }
    }
}
