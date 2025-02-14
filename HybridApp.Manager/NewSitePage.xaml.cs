using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using Microsoft.UI.Xaml.Media.Imaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HybridApp.Manager
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewSitePage : Page
    {
        public GeneralSettingsViewModel GeneralViewModel { get; set; }
        public IconSettingsViewModel IconViewModel { get; set; }
        private string customIconPath = "";

        public NewSitePage()
        {
            this.InitializeComponent();
            GeneralViewModel = new GeneralSettingsViewModel();
            GeneralViewModel.SetSettingData("StartMenuShortcut", true);
            GeneralViewModel.SetSettingData("DesktopShortcut", true);
            IconViewModel = new IconSettingsViewModel();
            this.DataContext = this;
        }

        private async void InstallSiteButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessingIcon.Visibility = Visibility.Visible;
            InstallSiteButton.IsEnabled = false;

            StringSettingItem WebsiteTitleItem = (StringSettingItem)GeneralViewModel.Settings[0];
            StringSettingItem WebsiteURLItem = (StringSettingItem)GeneralViewModel.Settings[1];
            BooleanSettingItem CreateStartMenuShortcutItem = (BooleanSettingItem)GeneralViewModel.Settings[2];
            BooleanSettingItem CreateDesktopShortcutItem = (BooleanSettingItem)GeneralViewModel.Settings[3];

            ErrorCreate.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(WebsiteTitleItem.Value) || string.IsNullOrWhiteSpace(WebsiteURLItem.Value))
            {
                string errorMsg = "The following fields are required: ";

                bool webTitleFailed = false;
                if (string.IsNullOrWhiteSpace(WebsiteTitleItem.Value))
                {
                    errorMsg += "Website title";
                    webTitleFailed = true;
                }

                if (string.IsNullOrWhiteSpace(WebsiteURLItem.Value))
                {
                    if (webTitleFailed)
                    {
                        errorMsg += ", ";
                    }
                    errorMsg += "Website URL";
                }

                ErrorCreate.Text = errorMsg;
                ErrorCreate.Visibility = Visibility.Visible;
                ProcessingIcon.Visibility = Visibility.Collapsed;
                InstallSiteButton.IsEnabled = true;
                return;
            }

            string processedUrl = InfoHelper.FixURLValidity(WebsiteURLItem.Value.Trim());
            string processedTitle = WebsiteTitleItem.Value.Trim();

            string tmpDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Temp", "HybridApp");
            Directory.CreateDirectory(tmpDir);

            bool result = true;
            RadioButtons? rbs = InfoHelper.GetRadioButtonsInItemsRepeater(IconRepeater, 0);
            if (rbs is null)
            {
                ErrorCreate.Text = "An error occurred during processing. Check website details and try again.";
                ErrorCreate.Visibility = Visibility.Visible;
                ProcessingIcon.Visibility = Visibility.Collapsed;
                InstallSiteButton.IsEnabled = true;
                return;
            }

            switch (rbs.SelectedIndex) 
            {
                case 0:
                    result = await InfoHelper.ProcessWebsiteIcon(processedUrl, tmpDir, false);
                    break;
                case 1:
                    result = await InfoHelper.ProcessWebsiteIcon(processedUrl, tmpDir, true);
                    break;
                case 2:
                    if (!string.IsNullOrWhiteSpace(customIconPath))
                    {
                        if (File.Exists(customIconPath))
                        {
                            InfoHelper.CopyToIcon(customIconPath, Path.Combine(tmpDir, "favicon.ico"));
                        }
                        else 
                        {
                            result = false;
                        }
                    }
                    else 
                    {
                        result = false;
                    }
                    break;
            }


            if (!result)
            {
                ErrorCreate.Text = "An error occurred during processing. Check website details and try again.";
                ErrorCreate.Visibility = Visibility.Visible;
                ProcessingIcon.Visibility = Visibility.Collapsed;
                InstallSiteButton.IsEnabled = true;
                return;
            }

            SiteConfiguration sc = new SiteConfiguration();
            sc.Name = processedTitle;
            sc.URL = processedUrl;
            sc.Icon = Path.Combine(tmpDir, "favicon.ico");
            sc.AdditionalAttributes.Add("CreateStartMenuShortcut", CreateStartMenuShortcutItem.IsEnabled ? "true" : "false");
            sc.AdditionalAttributes.Add("CreateDesktopShortcut", CreateDesktopShortcutItem.IsEnabled ? "true" : "false");
            sc.AdditionalAttributes.Add("WebsiteIconSourceType", rbs.SelectedIndex.ToString());

            ((App)Application.Current).MainSettingsManager.ConfigureNewSite(sc);
            Directory.Delete(tmpDir, true);  

            ProcessingIcon.Visibility = Visibility.Collapsed;
            InstallSiteButton.IsEnabled = true;

            ((App)Application.Current).SetMainNavViewIndex(2);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).SetMainNavViewIndex(0);
        }

        private async void IconSourceCustom_Click()
        {
            string file = await ((App)Application.Current).InvokeMainWindowImageSelectionDialog();

            if (file != "")
            {
                var iconItem = (IconTypeSettingItem)IconViewModel.Settings[0];
                iconItem.ImageSource = InfoHelper.CreateBitmapImage(new Uri(file));
                iconItem.ImageShown = Visibility.Visible;
                iconItem.PlaceholderShown = Visibility.Collapsed;
                customIconPath = file;
            }
            else 
            {
                var iconItem = (IconTypeSettingItem)IconViewModel.Settings[0];
                iconItem.SelectedIndex = 0;
                iconItem.ImageSource = null;
                iconItem.ImageShown = Visibility.Collapsed;
                iconItem.PlaceholderShown = Visibility.Visible;
                customIconPath = "";
            }
        }

        private async void IconSourceGoogle_Click() 
        {
            StringSettingItem WebsiteURLItem = (StringSettingItem)GeneralViewModel.Settings[1];
            if (!string.IsNullOrWhiteSpace(WebsiteURLItem.Value))
            {
                string processedUrl = InfoHelper.FixURLValidity(WebsiteURLItem.Value.Trim());

                string tmpDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Temp", "HybridApp");
                Directory.CreateDirectory(tmpDir);

                bool result = await InfoHelper.ProcessWebsiteIcon(processedUrl, tmpDir, true);

                if (!result)
                {
                    var iconItem = (IconTypeSettingItem)IconViewModel.Settings[0];
                    iconItem.ImageSource = null;
                    iconItem.ImageShown = Visibility.Collapsed;
                    iconItem.PlaceholderShown = Visibility.Visible;
                }
                else 
                {
                    var iconItem = (IconTypeSettingItem)IconViewModel.Settings[0];
                    iconItem.ImageSource = null;
                    iconItem.ImageSource = InfoHelper.CreateBitmapImage(new Uri(Path.Combine(tmpDir, "favicon.ico")));
                    iconItem.ImageShown = Visibility.Visible;
                    iconItem.PlaceholderShown = Visibility.Collapsed;
                }
            }
            else 
            {
                var iconItem = (IconTypeSettingItem)IconViewModel.Settings[0];
                iconItem.ImageSource = null;
                iconItem.ImageShown = Visibility.Collapsed;
                iconItem.PlaceholderShown = Visibility.Visible;
            }
        }

        private async void IconSourceWebsite_Click()
        {
            StringSettingItem WebsiteURLItem = (StringSettingItem)GeneralViewModel.Settings[1];
            if (!string.IsNullOrWhiteSpace(WebsiteURLItem.Value))
            {
                string processedUrl = InfoHelper.FixURLValidity(WebsiteURLItem.Value.Trim());

                string tmpDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Temp", "HybridApp");
                Directory.CreateDirectory(tmpDir);

                bool result = await InfoHelper.ProcessWebsiteIcon(processedUrl, tmpDir, false);

                if (!result)
                {
                    var iconItem = (IconTypeSettingItem)IconViewModel.Settings[0];
                    iconItem.ImageSource = null;
                    iconItem.ImageShown = Visibility.Collapsed;
                    iconItem.PlaceholderShown = Visibility.Visible;
                }
                else
                {
                    var iconItem = (IconTypeSettingItem)IconViewModel.Settings[0];
                    iconItem.ImageSource = null;
                    iconItem.ImageSource = InfoHelper.CreateBitmapImage(new Uri(Path.Combine(tmpDir, "favicon.ico")));
                    iconItem.ImageShown = Visibility.Visible;
                    iconItem.PlaceholderShown = Visibility.Collapsed;
                }
            }
            else
            {
                var iconItem = (IconTypeSettingItem)IconViewModel.Settings[0];
                iconItem.ImageSource = null;
                iconItem.ImageShown = Visibility.Collapsed;
                iconItem.PlaceholderShown = Visibility.Visible;
            }
        }

        private void IconTypeSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((RadioButtons)sender).SelectedIndex) 
            {
                case 0:
                    IconSourceWebsite_Click();
                    break;
                case 1:
                    IconSourceGoogle_Click();
                    break;
                case 2:
                    IconSourceCustom_Click();
                    break;
                default:
                    return;
            }
        }

        private void IconRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RadioButtons? rbs = InfoHelper.GetRadioButtonsInItemsRepeater(IconRepeater, 0);
            if (rbs is null)
                return;

            switch (rbs.SelectedIndex)
            {
                case 0:
                    IconSourceWebsite_Click();
                    break;
                case 1:
                    IconSourceGoogle_Click();
                    break;
                case 2:
                    IconSourceCustom_Click();
                    break;
                default:
                    return;
            }
        }
    }
}
