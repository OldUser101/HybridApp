using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
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
using SkiaSharp;
using System.Drawing.Imaging;
using System.Drawing;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HybridApp.Manager
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SiteSettingsPage : Page
    {
        public GeneralSettingsViewModel GeneralViewModel { get; set; } = new GeneralSettingsViewModel();
        public IconSettingsViewModel IconViewModel { get; set; } = new IconSettingsViewModel();
        private string customIconPath = "";
        private bool isInitialCustomIconSetting = true;
        private bool hasCustomIconChanged = false;
        private SiteConfiguration settingsConfig = new SiteConfiguration();

        public SiteSettingsPage()
        {
            SiteConfiguration? config = ((MainWindow)((App)Application.Current).m_window).settingsPageLastConfig;
            if (config is null)
            {
                ((App)Application.Current).SetMainNavViewIndex(0);
                return;
            }

            this.InitializeComponent();
            this.Loaded += SiteSettingsPage_Loaded;

            GeneralViewModel = new GeneralSettingsViewModel();
            IconViewModel = new IconSettingsViewModel();
            settingsConfig = config;
        }

        private void SiteSettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateAppRootConfig();
            this.DataContext = this;
        }

        private void UpdateAppRootConfig() 
        {
            AppTitle.Text = settingsConfig.Name;
            if (!string.IsNullOrWhiteSpace(settingsConfig.Icon))
            {
                RootIcon.Source = InfoHelper.CreateBitmapImage(new Uri(settingsConfig.Icon));
            }
            GeneralViewModel.SetSettingData("WebsiteName", settingsConfig.Name);
            GeneralViewModel.SetSettingData("WebsiteURL", settingsConfig.URL);
            ParseExtendedSettings();
        }

        private void ParseExtendedSettings() 
        {
            foreach (KeyValuePair<string, string> kv in settingsConfig.AdditionalAttributes) 
            {
                switch (kv.Key) 
                {
                    case "CreateStartMenuShortcut":
                        GeneralViewModel.SetSettingData("StartMenuShortcut", kv.Value == "true" ? true : false);
                        break;
                    case "CreateDesktopShortcut":
                        GeneralViewModel.SetSettingData("DesktopShortcut", kv.Value == "true" ? true : false);
                        break;
                    case "WebsiteIconSourceType":
                        ((IconTypeSettingItem)IconViewModel.Settings[0]).SelectedIndex = Convert.ToInt32(kv.Value);
                        break;
                    default:
                        continue;
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).MainContentFrameNavBack();
        }

        private async void SaveSiteButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessingIcon.Visibility = Visibility.Visible;
            SaveSiteButton.IsEnabled = false;

            StringSettingItem WebsiteTitleItem = (StringSettingItem)GeneralViewModel.Settings[0];
            StringSettingItem WebsiteURLItem = (StringSettingItem)GeneralViewModel.Settings[1];
            BooleanSettingItem CreateStartMenuShortcutItem = (BooleanSettingItem)GeneralViewModel.Settings[2];
            BooleanSettingItem CreateDesktopShortcutItem = (BooleanSettingItem)GeneralViewModel.Settings[3];

            ErrorSave.Visibility = Visibility.Collapsed;

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

                ErrorSave.Text = errorMsg;
                ErrorSave.Visibility = Visibility.Visible;
                ProcessingIcon.Visibility = Visibility.Collapsed;
                SaveSiteButton.IsEnabled = true;
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
                ErrorSave.Text = "An error occurred during processing. Check website details and try again.";
                ErrorSave.Visibility = Visibility.Visible;
                ProcessingIcon.Visibility = Visibility.Collapsed;
                SaveSiteButton.IsEnabled = true;
                return;
            }

            switch (rbs.SelectedIndex)
            {
                case 0:
                    if (File.Exists(Path.Combine(tmpDir, "favicon.ico"))) 
                    {
                        File.Delete(Path.Combine(tmpDir, "favicon.ico"));
                    }
                    result = await InfoHelper.ProcessWebsiteIcon(processedUrl, tmpDir, false);
                    break;
                case 1:
                    if (File.Exists(Path.Combine(tmpDir, "favicon.ico")))
                    {
                        File.Delete(Path.Combine(tmpDir, "favicon.ico"));
                    }
                    result = await InfoHelper.ProcessWebsiteIcon(processedUrl, tmpDir, true);
                    break;
                case 2:
                    if (!string.IsNullOrWhiteSpace(customIconPath))
                    {
                        if (File.Exists(customIconPath))
                        {
                            if (File.Exists(Path.Combine(tmpDir, "favicon.ico")))
                            {
                                File.Delete(Path.Combine(tmpDir, "favicon.ico"));
                            }

                            InfoHelper.CopyToIcon(customIconPath, Path.Combine(tmpDir, "favicon.ico"));
                        }
                        else
                        {
                            result = false;
                        }
                    }
                    else if (!hasCustomIconChanged)
                    {
                        result = true;
                    }
                    else 
                    {
                        result = false;
                    }
                    break;
            }

            if (!result)
            {
                ErrorSave.Text = "An error occurred during processing. Check website details and try again.";
                ErrorSave.Visibility = Visibility.Visible;
                ProcessingIcon.Visibility = Visibility.Collapsed;
                SaveSiteButton.IsEnabled = true;
                return;
            }

            SiteConfiguration sc = new SiteConfiguration();
            sc.Name = processedTitle;
            sc.URL = processedUrl;
            sc.Icon = Path.Combine(tmpDir, "favicon.ico");
            sc.ID = settingsConfig.ID;
            sc.QuickAccess = settingsConfig.QuickAccess;
            sc.AdditionalAttributes.Add("CreateStartMenuShortcut", CreateStartMenuShortcutItem.IsEnabled ? "true" : "false");
            sc.AdditionalAttributes.Add("CreateDesktopShortcut", CreateDesktopShortcutItem.IsEnabled ? "true" : "false");
            sc.AdditionalAttributes.Add("WebsiteIconSourceType", rbs.SelectedIndex.ToString());

            ((App)Application.Current).MainSettingsManager.ReConfigureSite(settingsConfig.ID, sc);
            Directory.Delete(tmpDir, true);

            ProcessingIcon.Visibility = Visibility.Collapsed;
            SaveSiteButton.IsEnabled = true;

            ((App)Application.Current).MainContentFrameNavBack();
        }

        private void LaunchSiteButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = Path.Combine(((App)Application.Current).MainSettingsManager.HOST_PATH, "HybridApp.Host.exe");
            psi.Arguments = settingsConfig.ID;
            psi.UseShellExecute = false;
            Process.Start(psi);
        }

        private void DeleteSiteButton_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).MainSettingsManager.DeleteSite(settingsConfig.ID);
            ((App)Application.Current).MainContentFrameNavBack();
        }

        private async void IconSourceCustom_Click()
        {
            if (isInitialCustomIconSetting)
            {
                if (!string.IsNullOrWhiteSpace(settingsConfig.Icon))
                {
                    var iconItem = (IconTypeSettingItem)IconViewModel.Settings[0];
                    iconItem.ImageSource = InfoHelper.CreateBitmapImage(new Uri(settingsConfig.Icon));
                    iconItem.ImageShown = Visibility.Visible;
                    iconItem.PlaceholderShown = Visibility.Collapsed;
                }
                return;
            }

            string file = await ((App)Application.Current).InvokeMainWindowImageSelectionDialog();

            if (file != "")
            {
                var iconItem = (IconTypeSettingItem)IconViewModel.Settings[0];
                iconItem.ImageSource = InfoHelper.CreateBitmapImage(new Uri(file));
                iconItem.ImageShown = Visibility.Visible;
                iconItem.PlaceholderShown = Visibility.Collapsed;
                customIconPath = file;
                hasCustomIconChanged = true;
                isInitialCustomIconSetting = false;
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
            isInitialCustomIconSetting = false;
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
            isInitialCustomIconSetting = false;
        }

        private void IconTypeSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((IconTypeSettingItem)(IconViewModel.Settings[0])).SelectedIndex)
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
            isInitialCustomIconSetting = false;
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
