using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Notifications;
using WinRT;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media.Imaging;
using HybridApp.Manager;
using Windows.Media;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;
using WinRT.Interop;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HybridApp.Host
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private static CustomSystemBackdrop? _customBackdrop;
        private static bool _initialActivation = false;
        public SiteConfiguration? desiredSiteConfig = null;
        private string lastGoodKnownPage = "";

        private nint _hwnd;
        private nint _oldWndProc;
        private WndProcDelegate _newWndProc;

        public MainWindow()
        {
            _initialActivation = true;
            this.InitializeComponent();

            _hwnd = WindowNative.GetWindowHandle(this);
            _newWndProc = WndProc;
            _oldWndProc = SetWindowLongPtr(_hwnd, -4, Marshal.GetFunctionPointerForDelegate(_newWndProc));

            this.Activated += OnWindowActivated;
            this.Closed += OnWindowClosed;
            AppTitleBar.Loaded += AppTitleBar_Loaded;
            AppTitleBar.SizeChanged += AppTitleBar_SizeChanged;
            MainWebView2.CoreWebView2Initialized += MainWebView2_CoreWebView2Initialized;
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

        private async void OnWindowClosed(object sender, WindowEventArgs e) 
        {
            if (MainWebView2 != null)
            {
                try
                {
                    await MainWebView2.EnsureCoreWebView2Async();
                    MainWebView2.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"WebView2 Cleanup error: {ex.Message}");
                }
                finally
                {
                    MainWebView2 = null;
                }
            }
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs e) 
        {
            if (_initialActivation)
            {               
                ExtendsContentIntoTitleBar = true;
                AppWindow thisWindow = this.AppWindow;
                thisWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

                if (desiredSiteConfig is null)
                {
                    thisWindow.SetIcon(@"Assets\HybridApp.Manager.ico");
                    thisWindow.Title = "HybridApp";
                }
                else
                {
                    ApplySiteConfiguration(desiredSiteConfig);
                }

                _customBackdrop = new CustomSystemBackdrop(CustomSystemBackdrop.ControllerMode.Acrylic);
                this.SystemBackdrop = _customBackdrop;

                BackButton.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(NavigationButton_PointerPressed), true);
                ForwardButton.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(NavigationButton_PointerPressed), true);
                RefreshButton.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(NavigationButton_PointerPressed), true);

                _initialActivation = false;

                SetTitleBar(AppTitleBar);
            }
        }

        private async void CoreWebView2_NewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
        {
            args.Handled = true;

            await Launcher.LaunchUriAsync(new Uri(args.Uri));
        }

        private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (ExtendsContentIntoTitleBar == true)
            {
                SetRegionsForCustomTitleBar();
            }
        }

        private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ExtendsContentIntoTitleBar == true)
            {
                SetRegionsForCustomTitleBar();
            }
        }

        private void SetRegionsForCustomTitleBar()
        {
            double scaleAdjustment = AppTitleBar.XamlRoot.RasterizationScale;
            AppWindow thisWindow = this.AppWindow;

            RightPaddingColumn.Width = new GridLength(thisWindow.TitleBar.RightInset / scaleAdjustment);
            LeftPaddingColumn.Width = new GridLength(thisWindow.TitleBar.LeftInset / scaleAdjustment);
        }

        private void MainWebView2_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            var coreWebView2 = sender.CoreWebView2;
            coreWebView2.NavigationCompleted += MainWebView2_NavigationComplete;
           // coreWebView2.NavigationStarting += MainWebView2_NavigationStarting;
            coreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
        }

        private void CoreWebView2_DOMContentLoaded(CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs args)
        {
            ApplyThemeColorToBackdrop();
        }

        private async void ApplySiteConfiguration(SiteConfiguration config) 
        {
            AppWindow thisWindow = this.AppWindow;
            thisWindow.SetIcon(config.Icon);
            thisWindow.Title = config.Name;
            MainWebView2.Source = new Uri(config.URL);
            FaviconImage.Source = new BitmapImage(new Uri(config.Icon));
            PageTitle.Text = config.Name;
            lastGoodKnownPage = config.URL;
            string userDataFolder = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HybridApp"), "sites", desiredSiteConfig.ID, "data");
            if (!Directory.Exists(userDataFolder))
            {
                Directory.CreateDirectory(userDataFolder);
            }
            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", userDataFolder, EnvironmentVariableTarget.Process);
            await MainWebView2.EnsureCoreWebView2Async();
        }

        private void MainWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args) 
        {
            return;
            if (desiredSiteConfig is null)
                return;

            if (!args.Uri.StartsWith(desiredSiteConfig.URL) && !args.IsRedirected) 
            {
                MainWebView2.CoreWebView2.Stop();

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.FileName = "explorer.exe";
                psi.Arguments = $"\"{args.Uri}\"";
                Process.Start(psi);

                if (MainWebView2.CoreWebView2.CanGoBack)
                {
                    MainWebView2.CoreWebView2.GoBack();
                }
                else
                {
                    MainWebView2.CoreWebView2.Navigate(lastGoodKnownPage);
                }
                return;
            }
            lastGoodKnownPage = args.Uri;
        }

        private async void MainWebView2_NavigationComplete(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args) 
        {
            if (desiredSiteConfig is null)
            {
                string pageTitle = await MainWebView2.ExecuteScriptAsync("document.title");
                string faviconUrl = await MainWebView2.ExecuteScriptAsync("document.querySelector(\"link[rel='icon']\") ? document.querySelector(\"link[rel='icon']\").href : ''");
                if (faviconUrl.Trim('"') != "")
                {
                    FaviconImage.Source = new BitmapImage(new Uri(faviconUrl.Trim('"')));
                }
                else
                {
                    FaviconImage.Source = new BitmapImage(new Uri($"https://www.google.com/s2/favicons?domain={MainWebView2.Source}&size=256"));
                }
                PageTitle.Text = pageTitle.Trim('"');
            }
            ApplyThemeColorToBackdrop();
        }

        private async void ApplyThemeColorToBackdrop()
        {
            try
            {
                string script = @"
        function getThemeColor() {
            let themeColor = getComputedStyle(document.documentElement).getPropertyValue('--theme-color').trim();
            if (!themeColor) {
                themeColor = document.querySelector('meta[name=""theme-color""]');
                if (themeColor) {
                    return themeColor.getAttribute('content');
                }
            }
            if (!themeColor) {
                themeColor = window.getComputedStyle(document.body).backgroundColor;
            }
            return themeColor;
        }
        getThemeColor();";

                string themeColor = await MainWebView2.CoreWebView2.ExecuteScriptAsync(script);

                if (!string.IsNullOrEmpty(themeColor))
                {
                    Color? computedColor = ParseColorString(themeColor);
                    if (computedColor is not null)
                    {
                        SetDisplayThemeColors((Color)computedColor);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying theme color: {ex.Message}");
            }
        }

        private Color? ParseColorString(string colorString)
        {
            colorString = colorString.Trim('"');

            if (colorString.StartsWith("rgb"))
            {
                var parts = colorString.Replace("rgb(", "").Replace(")", "").Split(',');

                if (parts.Length == 3)
                {
                    byte r = byte.Parse(parts[0].Trim());
                    byte g = byte.Parse(parts[1].Trim());
                    byte b = byte.Parse(parts[2].Trim());
                    return Color.FromArgb(255, r, g, b);
                }
            }
            else if (colorString.StartsWith('#'))
            {
                Color computed = new Color();
                UInt32 colorData = Convert.ToUInt32(colorString.Remove(0, 1), 16);
                computed.A = 255;
                computed.R = (byte)(colorData >> 16);
                computed.G = (byte)((colorData >> 8) & 0xFF);
                computed.B = (byte)(colorData & 0xFF);
                return computed;
            }

            return null;
        }

        private void SetDisplayThemeColors(Color color) 
        {
            if (_customBackdrop is null)
                return;

            _customBackdrop.SetControllerTintColor(color);
            _customBackdrop.SetControllerTintOpacity(0.75f);

            Color modified1;
            Color modified2;

            double luminance = CalculateLuminance(color);

            if (luminance <= 0.5)
            {
                modified1 = LightenColor(color, 0.1f);
                modified2 = LightenColor(color, 0.2f);              
                MainContent.RequestedTheme = ElementTheme.Dark;
            }
            else
            {
                modified1 = DarkenColor(color, 0.2f);
                modified2 = DarkenColor(color, 0.1f);
                MainContent.RequestedTheme = ElementTheme.Light;
            }

            AppWindowTitleBar thisTitleBar = this.AppWindow.TitleBar;
            thisTitleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
            thisTitleBar.ButtonHoverBackgroundColor = modified2;
            thisTitleBar.ButtonPressedBackgroundColor = modified1;
            thisTitleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);

            if (luminance <= 0.5)
            {
                thisTitleBar.ButtonForegroundColor = Color.FromArgb(255, 255, 255, 255);
                thisTitleBar.ButtonHoverForegroundColor = Color.FromArgb(255, 255, 255, 255);
                thisTitleBar.ButtonPressedForegroundColor = Color.FromArgb(255, 255, 255, 255);
                ((SolidColorBrush)Application.Current.Resources["TransparentButtonPressedForegroundBrush"]).Color = Color.FromArgb(255, 255, 255, 255);
            }
            else 
            {
                thisTitleBar.ButtonForegroundColor = Color.FromArgb(255, 0, 0, 0);
                thisTitleBar.ButtonHoverForegroundColor = Color.FromArgb(255, 0, 0, 0);
                thisTitleBar.ButtonPressedForegroundColor = Color.FromArgb(255, 0, 0, 0);
                ((SolidColorBrush)Application.Current.Resources["TransparentButtonPressedForegroundBrush"]).Color = Color.FromArgb(255, 0, 0, 0);
            }

            ((SolidColorBrush)Application.Current.Resources["TransparentButtonPointerOverBackgroundBrush"]).Color = modified2;
            ((SolidColorBrush)Application.Current.Resources["TransparentButtonPressedBackgroundBrush"]).Color = modified1;           
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

        public static double CalculateLuminance(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWebView2.CoreWebView2 != null && MainWebView2.CanGoBack)
            {
                MainWebView2.GoBack();
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWebView2.CoreWebView2 != null && MainWebView2.CanGoForward)
            {
                MainWebView2.GoForward();
            }
        }

        private void NavigationButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState((Button)sender, "ButtonPointerPressed", true);
        }

        private void NavigationButton_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState((Button)sender, "ButtonPointerOver", true);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            MainWebView2.Reload();
        }
    }
}
