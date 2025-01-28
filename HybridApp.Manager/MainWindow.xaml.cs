using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HybridApp.Manager
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private List<string> _items;
        private bool _isInitialNavigation;
        private bool _isInitialActivation;
        public SiteConfiguration? settingsPageLastConfig = null;

        public MainWindow()
        {
            this.InitializeComponent();

            _isInitialActivation = true;
            Activated += MainWindow_Activated;

            _isInitialNavigation = true;
            NavView.SelectedItem = HomeItem;

            _items = new List<string>();
        }

        public void ContentNavigateToSettingsPage(SiteConfiguration config) 
        {
            settingsPageLastConfig = config;
            ContentFrame.Navigate(typeof(SiteSettingsPage));
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_isInitialActivation) 
            {
                ExtendsContentIntoTitleBar = true;
                AppWindow thisWindow = this.AppWindow;
                thisWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
                thisWindow.SetIcon(@"Assets\HybridApp.Manager.ico");
                thisWindow.Title = "HybridApp Manager";
                SetTitleBar(AppTitleBar);
                _isInitialActivation = false;
            }
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                TitleBarTextBlock.Foreground =
                    (SolidColorBrush)App.Current.Resources["WindowCaptionForegroundDisabled"];
            }
            else
            {
                TitleBarTextBlock.Foreground =
                    (SolidColorBrush)App.Current.Resources["WindowCaptionForeground"];
            }
        }

        private void NavView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            AppTitleBar.Margin = new Thickness()
            {
                Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
                Top = AppTitleBar.Margin.Top,
                Right = AppTitleBar.Margin.Right,
                Bottom = AppTitleBar.Margin.Bottom
            };
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args) 
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
            }
        }

        public void SwitchView(int index)
        {
            NavView.SelectedItem = NavView.MenuItems[index];
        }

        public void SwitchViewFooter(int index)
        {
            NavView.SelectedItem = NavView.FooterMenuItems[index];
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            NavigationViewItem selectedItem = (NavigationViewItem)args.SelectedItem;
            if (selectedItem is not null) 
            {
                string tag = selectedItem.Tag.ToString() ?? String.Empty;

                switch (tag) 
                {
                    case "HomePage":
                        ContentFrame.Navigate(typeof(HomePage));
                        break;
                    case "QuickAccessPage":
                        ContentFrame.Navigate(typeof(QuickAccessPage));
                        break;
                    case "InstalledSitesPage":
                        ContentFrame.Navigate(typeof(InstalledSitesPage));
                        break;
                    case "NewSitePage":
                        ContentFrame.Navigate(typeof(NewSitePage));
                        break;
                    case "SettingsPage":
                        ContentFrame.Navigate(typeof(SettingsPage));
                        break;
                    case "HelpPage":
                        ContentFrame.Navigate(typeof(HelpPage));
                        break;
                    case "AboutPage":
                        ContentFrame.Navigate(typeof(AboutPage));
                        break;
                    case "ExitPage":
                        Application.Current.Exit();
                        break;
                }

                UpdateBackButtonState();
            }
        }

        private void UpdateBackButtonState()
        {
            if (!_isInitialNavigation)
                NavView.IsBackEnabled = ContentFrame.CanGoBack;
            _isInitialNavigation = false;
        }

        public void ContentNavBack() 
        {
            if (ContentFrame.CanGoBack) 
            {
                ContentFrame.GoBack();
            }
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            Type currentPage = e.SourcePageType;

            foreach (NavigationViewItem item in NavView.MenuItems)
            {
                if (item.Tag as string == currentPage.Name)
                {
                    NavView.SelectedItem = item;
                    break;
                }
            }

            foreach (NavigationViewItem item in NavView.FooterMenuItems)
            {
                if (item.Tag as string == currentPage.Name)
                {
                    NavView.SelectedItem = item;
                    break;
                }
            }
        }

        public async Task<string> InvokeImageSelectionDialog() 
        {
            var filePicker = new FileOpenPicker();
            InitializeWithWindow.Initialize(filePicker, WinRT.Interop.WindowNative.GetWindowHandle(this));

            filePicker.FileTypeFilter.Add(".bmp");
            filePicker.FileTypeFilter.Add(".ico");
            filePicker.FileTypeFilter.Add(".jpg");
            filePicker.FileTypeFilter.Add(".jpeg");
            filePicker.FileTypeFilter.Add(".png");
            filePicker.FileTypeFilter.Add(".gif");
            filePicker.FileTypeFilter.Add(".webp");

            var file = await filePicker.PickSingleFileAsync();

            if (file is not null)
                return file.Path;
            return "";
        } 
    }
}
