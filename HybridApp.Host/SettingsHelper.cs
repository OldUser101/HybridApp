using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;

namespace HybridApp.Manager
{
    public class HybridAppConfiguration
    {
        public string SiteDirectory { get; set; }
        public List<Site> Sites { get; set; }
    }

    public class Site
    {
        public string Name { get; set; }
        public string URL { get; set; }
        public string Icon { get; set; }
        public string ID { get; set; }
    }

    public class SiteConfiguration
    {
        public string Name { get; set; }
        public string URL { get; set; }
        public string ID { get; set; }
        public string Icon { get; set; }
        public Dictionary<string, string> AdditionalAttributes { get; set; } = new Dictionary<string, string>();
    }


    internal class SettingsHelper
    {
        public static string ResolveIconPath(string? siteDirectory, string? id, string? icon)
        {
            if (string.IsNullOrEmpty(siteDirectory) || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(icon))
            {
                return String.Empty;
            }

            return Path.Combine(siteDirectory, id, icon);
        }

        public static void SetUpDirectories(string hostPath)
        {
            string localPath = hostPath;
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }

            string configFile = Path.Combine(localPath, "config.xml");
            if (!File.Exists(configFile))
            {
                SetUpDefaultConfigFile(localPath);
            }
        }

        public static void SetUpDefaultConfigFile(string localPath)
        {
            HybridAppConfiguration hac = new HybridAppConfiguration();

            string siteDir = Path.Combine(localPath, "sites");
            if (!Directory.Exists(siteDir))
            {
                Directory.CreateDirectory(siteDir);
            }

            hac.SiteDirectory = siteDir;
            hac.Sites = new List<Site>();

            SettingsHelper.WriteConfiguration(Path.Combine(localPath, "config.xml"), hac);
        }

        public static void WriteConfiguration(string filePath, HybridAppConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (configuration.Sites == null)
                throw new ArgumentException("Sites list cannot be null.", nameof(configuration.Sites));

            var hybridAppSites = new XElement("HybridAppSites",
                new XAttribute("SiteDirectory", configuration.SiteDirectory ?? string.Empty),
                configuration.Sites?.ConvertAll(site =>
                    new XElement("Site",
                        new XAttribute("Name", site.Name ?? string.Empty),
                        new XAttribute("URL", site.URL ?? string.Empty),
                        new XAttribute("Icon", site.Icon ?? string.Empty),
                        new XAttribute("ID", site.ID ?? string.Empty)
                    )
                )
            );

            var root = new XElement("HybridAppConfiguration", hybridAppSites);

            var xdoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
            xdoc.Save(filePath);
        }

        public static HybridAppConfiguration ParseConfiguration(string xmlPath)
        {
            var xdoc = XDocument.Load(xmlPath);

            var SiteDirectory = xdoc.Descendants("HybridAppSites")
                                .FirstOrDefault()?
                                .Attribute("SiteDirectory")?.Value;

            var Sites = xdoc.Descendants("Site")
                         .Select(site => new Site
                         {
                             Name = site.Attribute("Name")?.Value,
                             URL = site.Attribute("URL")?.Value,
                             Icon = ResolveIconPath(SiteDirectory, site.Attribute("ID")?.Value, site.Attribute("Icon")?.Value),
                             ID = site.Attribute("ID")?.Value
                         })
                         .ToList();


            return new HybridAppConfiguration
            {
                SiteDirectory = SiteDirectory,
                Sites = Sites
            };
        }

        public static SiteConfiguration ParseSiteConfiguration(string xmlFilePath, string siteDirectory)
        {
            XDocument doc = XDocument.Load(xmlFilePath);
            XElement root = doc.Root;

            if (root == null || root.Name != "SiteConfiguration")
            {
                throw new InvalidOperationException("Invalid XML format: Root element must be 'SiteConfiguration'.");
            }

            SiteConfiguration config = new SiteConfiguration
            {
                Name = root.Attribute("Name")?.Value,
                URL = root.Attribute("URL")?.Value,
                ID = root.Attribute("ID")?.Value,
                Icon = ResolveIconPath(siteDirectory, root.Attribute("ID")?.Value, root.Attribute("Icon")?.Value)
            };

            foreach (var attribute in root.Attributes())
            {
                string attributeName = attribute.Name.LocalName;
                if (attributeName != "Name" && attributeName != "URL" && attributeName != "ID" && attributeName != "Icon")
                {
                    config.AdditionalAttributes[attributeName] = attribute.Value;
                }
            }

            return config;
        }

        public static void WriteSiteConfiguration(string xmlFilePath, SiteConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "The SiteConfiguration object cannot be null.");
            }

            XElement root = new XElement("SiteConfiguration",
                new XAttribute("Name", config.Name ?? string.Empty),
                new XAttribute("URL", config.URL ?? string.Empty),
                new XAttribute("ID", config.ID ?? string.Empty),
                new XAttribute("Icon", config.Icon ?? string.Empty)
            );

            foreach (var kvp in config.AdditionalAttributes)
            {
                root.SetAttributeValue(kvp.Key, kvp.Value);
            }

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                root
            );

            doc.Save(xmlFilePath);
        }
    }
}
