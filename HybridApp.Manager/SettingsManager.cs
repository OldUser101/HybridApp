using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Linq.Expressions;
using ABI.System;
using System.Xml.Linq;
using System.Collections;

namespace HybridApp.Manager
{
    public class SettingsManager
    {
        private HybridAppConfiguration? config;
        private int nextId;
        public int errorLevel;
        public string HOST_PATH;

        public SettingsManager() 
        {
            this.errorLevel = 0;

            this.HOST_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HybridApp");

            SettingsHelper.SetUpDirectories(this.HOST_PATH);
            this.config = SettingsHelper.ParseConfiguration(Path.Combine(this.HOST_PATH, "config.xml"));

            if (this.config is null)
            {
                errorLevel = 1;
                return;
            }
            else
            {
                this.nextId = GetNextId();
                if (this.nextId == -1)
                {
                    this.errorLevel = 1;
                }
            }
        }

        private int GetNextId() 
        {
            if (this.config is null || this.errorLevel == 1) {
                return -1;
            }

            int highest = 0;
            foreach (Site s in config.Sites) 
            {
                int id = Convert.ToInt32(s.ID.Remove(0, 3));
                if (id > highest) {
                    highest = id;
                }
            }

            if (highest == 9999)
                return -1;

            return highest + 1;
        }

        public void SiteRootConfigUpdateQuickAccessState(string id, bool state) 
        {
            if (errorLevel == 1 || config is null)
                return;

            for (int i = 0; i < config.Sites.Count; i++) 
            {
                if (config.Sites[i].ID == id) 
                {
                    config.Sites[i].QuickAccess = state;
                    SettingsHelper.WriteConfiguration(Path.Combine(this.HOST_PATH, "config.xml"), config);
                    return;
                }
            }
        }

        public void DeleteSite(string id) 
        {
            if (errorLevel == 1 || config is null)
                return;

            Site? target = null;

            foreach (Site s in config.Sites)
            {
                if (s.ID == id)
                {
                    target = s;
                }
            }

            if (target is not null) 
            {
                config.Sites.Remove(target);
                SettingsHelper.WriteConfiguration(Path.Combine(this.HOST_PATH, "config.xml"), config);
                Directory.Delete(Path.Combine(this.config.SiteDirectory, target.ID), true);
                HandleShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", $"{target.Name}.lnk"), false, null);
                HandleShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{target.Name}.lnk"), false, null);
            }
        }

        public List<Site> GetSitesList()
        {
            if (this.config is null || this.errorLevel == 1) {
                return new List<Site>();
            }
            return config.Sites;
        }

        public SiteConfiguration GetSiteConfig(string id) 
        {
            if (errorLevel == 1 || config is null)
                return new SiteConfiguration();

            foreach (Site s in config.Sites) 
            {
                if (s.ID == id)
                {
                    SiteConfiguration sc = SettingsHelper.ParseSiteConfiguration(Path.Combine(config.SiteDirectory, s.ID, "site.xml"), config.SiteDirectory);
                    return sc;
                }
            }

            this.errorLevel = 1;
            return new SiteConfiguration();
        }

        public void ReConfigureSite(string ID, SiteConfiguration newConfig, bool onlyCritical = false) 
        {
            if (errorLevel == 1 || config is null)
                return;

            Site s = new Site();
            s.Name = newConfig.Name;
            s.URL = newConfig.URL;
            s.ID = ID;
            s.QuickAccess = newConfig.QuickAccess;

            if (!onlyCritical)
            {
                ProcessExtendedFlags(newConfig);
            }

            if (File.Exists(newConfig.Icon) && !onlyCritical)
            {
                string iconPath = SettingsHelper.ResolveIconPath(config.SiteDirectory, s.ID, Path.GetFileName(newConfig.Icon));
                File.Copy(newConfig.Icon, iconPath, true);
                s.Icon = iconPath;
                newConfig.Icon = Path.GetFileName(newConfig.Icon);
            }
            else
            {
                string iconPath = SettingsHelper.ResolveIconPath(config.SiteDirectory, s.ID, Path.GetFileName(newConfig.Icon));
                s.Icon = iconPath;
                newConfig.Icon = Path.GetFileName(newConfig.Icon);
            }

            for (int i = 0; i < this.config.Sites.Count(); i++) 
            {
                if (this.config.Sites[i].ID == ID) 
                {
                    this.config.Sites[i] = s;
                    break;
                }
            }

            SettingsHelper.WriteConfiguration(Path.Combine(this.HOST_PATH, "config.xml"), config);
            SettingsHelper.WriteSiteConfiguration(Path.Combine(config.SiteDirectory, ID, "site.xml"), newConfig);
        }

        public void ConfigureNewSite(SiteConfiguration newConfig) 
        {
            if (errorLevel == 1 || config is null)
                return;

            Site s = new Site();
            s.Name = newConfig.Name;
            s.URL = newConfig.URL;
            s.ID = "ID_" + this.nextId.ToString("D4");

            newConfig.ID = s.ID;

            Directory.CreateDirectory(Path.Combine(config.SiteDirectory, s.ID));
            Directory.CreateDirectory(Path.Combine(config.SiteDirectory, s.ID, "data"));

            ProcessExtendedFlags(newConfig);

            if (File.Exists(newConfig.Icon))
            {
                string iconPath = SettingsHelper.ResolveIconPath(config.SiteDirectory, s.ID, Path.GetFileName(newConfig.Icon));
                File.Copy(newConfig.Icon, iconPath, true);
                s.Icon = iconPath;
                newConfig.Icon = Path.GetFileName(newConfig.Icon);
            }
            else 
            {
                s.Icon = "";
                newConfig.Icon = "";
            }

            this.config.Sites.Add(s);
            SettingsHelper.WriteConfiguration(Path.Combine(this.HOST_PATH, "config.xml"), config);
            SettingsHelper.WriteSiteConfiguration(Path.Combine(config.SiteDirectory, s.ID, "site.xml"), newConfig);
            this.nextId++;
        }

        private void ProcessExtendedFlags(SiteConfiguration sc) 
        {
            if (errorLevel == 1 || config is null)
                return;

            foreach (KeyValuePair<string, string> kv in sc.AdditionalAttributes) 
            {
                switch (kv.Key) 
                {
                    case "CreateStartMenuShortcut":
                        HandleShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", $"{sc.Name}.lnk"), kv.Value == "true" ? true : false, sc);
                        break;
                    case "CreateDesktopShortcut":
                        HandleShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{sc.Name}.lnk"), kv.Value == "true" ? true : false, sc);
                        break;
                    default:
                        continue;
                }
            }
        }

        private void HandleShortcut(string path, bool bEnabled, SiteConfiguration? sc) 
        {
            if (bEnabled)
            {
                if (sc is null || this.config is null)
                {
                    return;
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                if (!string.IsNullOrEmpty(sc.Icon))
                {
                    string iconPath = SettingsHelper.ResolveIconPath(this.config.SiteDirectory, sc.ID, Path.GetFileName(sc.Icon));
                    ShortcutHelper.CreateShortcut(path, $"{this.HOST_PATH}\\HybridApp.Host.exe", iconPath, sc.ID);
                }
                else 
                {
                    ShortcutHelper.CreateShortcut(path, $"{this.HOST_PATH}\\HybridApp.Host.exe", "", sc.ID);
                }
            }
            else if (!bEnabled && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public void ConfigureNewSite(string name, string URL, string currentIcon) 
        {
            if (errorLevel == 1 || config is null)
                return;

            Site s = new Site();
            s.Name = name;
            s.URL = URL;
            s.ID = "ID_" + this.nextId.ToString("D4");

            SiteConfiguration sc = new SiteConfiguration();
            sc.Name = name;
            sc.URL = URL;
            sc.ID = s.ID;

            string iconPath = SettingsHelper.ResolveIconPath(config.SiteDirectory, s.ID, Path.GetFileName(currentIcon));

            s.Icon = iconPath;
            sc.Icon = Path.GetFileName(currentIcon);

            Directory.CreateDirectory(Path.Combine(config.SiteDirectory, s.ID));
            Directory.CreateDirectory(Path.Combine(config.SiteDirectory, s.ID, "data"));
            File.Copy(currentIcon, iconPath);

            this.config.Sites.Add(s);
            SettingsHelper.WriteConfiguration(Path.Combine(this.HOST_PATH, "config.xml"), config);
            SettingsHelper.WriteSiteConfiguration(Path.Combine(config.SiteDirectory, s.ID, "site.xml"), sc);
            this.nextId++;
        }
    }
}
