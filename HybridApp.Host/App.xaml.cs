﻿using HybridApp.Manager;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HybridApp.Host
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    /// 
    public partial class App : Application
    {
        public SettingsManager MainSettingsManager { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            AUMIDHelper.SetCurrentProcessExplicitAppUserModelID($"HybridApp_Host_{Guid.NewGuid()}");
            this.InitializeComponent();
            MainSettingsManager = new SettingsManager();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            string[] cmdargs = Environment.GetCommandLineArgs();
            SiteConfiguration? desiredConfig = null;
            foreach (string arg in cmdargs) 
            {
                string parsed = arg.Trim();
                if (parsed.StartsWith("ID_")) 
                {
                    SiteConfiguration sc = MainSettingsManager.GetSiteConfig(parsed);
                    if (MainSettingsManager.errorLevel != 1)
                    {
                        desiredConfig = sc;
                    }
                    else 
                    {
                        MainSettingsManager.errorLevel = 0;
                        break;
                    }
                }
            }

            m_window = new MainWindow();

            ((MainWindow)m_window).desiredSiteConfig = desiredConfig;

            m_window.Activate();
        }

        private Window? m_window;
    }
}
