using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Linq.Expressions;

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
            if (this.config is null || this.errorLevel == 1)
            {
                return -1;
            }

            int highest = 0;
            foreach (Site s in config.Sites)
            {
                int id = Convert.ToInt32(s.ID.Remove(0, 3));
                if (id > highest)
                {
                    highest = id;
                }
            }

            if (highest == 9999)
                return -1;

            return highest + 1;
        }

        public List<Site> GetSitesList()
        {
            if (this.config is null || this.errorLevel == 1)
            {
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

    }
}
