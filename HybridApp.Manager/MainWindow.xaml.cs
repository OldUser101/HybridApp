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
using System.Diagnostics;
using Windows.UI;

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
        private bool needsUpdate;

        private nint _hwnd;
        private nint _oldWndProc;
        private WndProcDelegate _newWndProc;

        public MainWindow(bool promptForUpdate = false)
        {
            this.needsUpdate = promptForUpdate;

            this.InitializeComponent();

            _hwnd = WindowNative.GetWindowHandle(this);
            _newWndProc = WndProc;
            _oldWndProc = SetWindowLongPtr(_hwnd, -4, Marshal.GetFunctionPointerForDelegate(_newWndProc));

            _isInitialActivation = true;
            Activated += MainWindow_Activated;

            _isInitialNavigation = true;
            NavView.SelectedItem = HomeItem;

            _items = new List<string>();
        }

        private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

        private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
        {
            const uint WM_GETMINMAXINFO = 0x0024;

            if (msg == WM_GETMINMAXINFO)
            {
                MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);

                minMaxInfo.ptMinTrackSize.X = 780;
                minMaxInfo.ptMinTrackSize.Y = 520;

                Marshal.StructureToPtr(minMaxInfo, lParam, true);
                return 0;
            }

            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

        [DllImport("user32.dll")]
        private static extern nint CallWindowProc(nint lpPrevWndFunc, nint hWnd, uint msg, nint wParam, nint lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
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

                Color modified1;
                Color modified2;

                Color WHITE = Color.FromArgb(255, 255, 255, 255);
                Color BLACK = Color.FromArgb(255, 0, 0, 0);

                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                {

                    modified1 = LightenColor(BLACK, 0.1f);
                    modified2 = LightenColor(BLACK, 0.2f);
                }
                else
                {
                    modified1 = DarkenColor(WHITE, 0.2f);
                    modified2 = DarkenColor(WHITE, 0.1f);
                }

                AppWindowTitleBar thisTitleBar = this.AppWindow.TitleBar;
                thisTitleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
                thisTitleBar.ButtonHoverBackgroundColor = modified2;
                thisTitleBar.ButtonPressedBackgroundColor = modified1;
                thisTitleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);

                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                {
                    thisTitleBar.ButtonForegroundColor = Color.FromArgb(255, 255, 255, 255);
                    thisTitleBar.ButtonHoverForegroundColor = Color.FromArgb(255, 255, 255, 255);
                    thisTitleBar.ButtonPressedForegroundColor = Color.FromArgb(255, 255, 255, 255);
                }
                else
                {
                    thisTitleBar.ButtonForegroundColor = Color.FromArgb(255, 0, 0, 0);
                    thisTitleBar.ButtonHoverForegroundColor = Color.FromArgb(255, 0, 0, 0);
                    thisTitleBar.ButtonPressedForegroundColor = Color.FromArgb(255, 0, 0, 0);
                }

                _isInitialActivation = false;
            }

            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                TitleBarTextBlock.Foreground = (SolidColorBrush)App.Current.Resources["WindowCaptionForegroundDisabled"];
            }
            else
            {
                TitleBarTextBlock.Foreground = (SolidColorBrush)App.Current.Resources["WindowCaptionForeground"];
            }
        }

        public static Color LightenColor(Color color, float factor)
        {
            byte r = (byte)Math.Min(255, color.R + (255 - color.R) * factor);
            byte g = (byte)Math.Min(255, color.G + (255 - color.G) * factor);
            byte b = (byte)Math.Min(255, color.B + (255 - color.B) * factor);

            return Color.FromArgb(color.A, r, g, b);
        }

        public static Color DarkenColor(Color color, float factor)
        {
            byte r = (byte)Math.Max(0, color.R - color.R * factor);
            byte g = (byte)Math.Max(0, color.G - color.G * factor);
            byte b = (byte)Math.Max(0, color.B - color.B * factor);

            return Color.FromArgb(color.A, r, g, b);
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

        public void SetTheme(ApplicationTheme theme) 
        {
            App.Current.RequestedTheme = theme;
        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            if (needsUpdate)
            {
                ContentDialog dialog = new ContentDialog();

                dialog.XamlRoot = this.Content.XamlRoot;
                dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                dialog.Title = "Update?";
                dialog.PrimaryButtonText = "Update Now";
                dialog.CloseButtonText = "Close";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = new UpdatePage();

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary) 
                {
                    this.AppWindow.Hide();
                    string targetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Temp", "HybridApp");
                    Directory.CreateDirectory(targetPath);
                    targetPath = Path.Combine(targetPath, "latest.msi");
                    if (UpdateHelper.DownloadUpdate(targetPath)) 
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.CreateNoWindow = true;
                        psi.UseShellExecute = false;
                        psi.Arguments = $"-i \"{targetPath}\"";
                        psi.FileName = "msiexec.exe";
                        Process.Start(psi);
                        Application.Current.Exit();
                    }
                }
            }
        }

        private void NavigationViewItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            NavigationViewItem? item = sender as NavigationViewItem;
            if (item is not null) 
            {
                item.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));   
            }
        }

        private void NavigationViewItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            NavigationViewItem? item = sender as NavigationViewItem;
            if (item is not null)
            {
                item.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            }
        }

        private void NavigationViewItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            NavigationViewItem? item = sender as NavigationViewItem;
            if (item is not null)
            {
                item.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            }
        }
    }
}
