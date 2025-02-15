using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Threading.Tasks;
using System.Drawing;
using System.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HybridApp.Manager
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public SettingsManager MainSettingsManager { get; private set; }
        private bool needsUpdate = false;
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            MainSettingsManager = new SettingsManager();

            ApplicationTheme? requestedTheme = null;

            Dictionary<string, string> options = MainSettingsManager.GetAdditionalOptions();
            if (options.Keys.Contains("ApplicationTheme"))
            {
                int theme = Convert.ToInt32(options["ApplicationTheme"]);

                if (theme == 0 || theme == 1) 
                {
                    requestedTheme = (ApplicationTheme)theme;
                }
                else 
                {
                    MainSettingsManager.AddOption("ApplicationTheme", "2");
                    MainSettingsManager.WriteConfig();
                }
            }
            
            if (requestedTheme is not null)
            {
                this.RequestedTheme = requestedTheme ?? ApplicationTheme.Light;
            }

            //if (UpdateHelper.CheckForUpdate(1000)) 
            //{
            //    needsUpdate = true;
            //}         
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow(this.needsUpdate);
            m_window.Activate();
        }

        public async Task<string> InvokeMainWindowImageSelectionDialog() 
        {
            if (m_window is null)
                return "";

            return await ((MainWindow)m_window).InvokeImageSelectionDialog();
        }

        public void SetMainNavViewIndex(int index) {
            if (m_window is null)
                return;

            ((MainWindow)m_window).SwitchView(index);
        }

        public void SetMainNavViewIndexFooter(int index)
        {
            if (m_window is null)
                return;

            ((MainWindow)m_window).SwitchViewFooter(index);
        }

        public void MainContentFrameNavBack() 
        {
            if (m_window is null)
                return;

            ((MainWindow)m_window).ContentNavBack();
        }

        public Window? m_window;
    }
}
