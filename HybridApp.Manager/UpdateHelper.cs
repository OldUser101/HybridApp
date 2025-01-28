using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace HybridApp.Manager
{
    public class UpdateHelper
    {
        public static bool CheckForUpdate(string currentVersionString) 
        {
            using (WebClient wc = new WebClient())
            {
                byte[] vString = wc.DownloadData(new Uri("https://raw.githubusercontent.com/OldUser101/HybridApp/refs/heads/master/LICENSE"));
                string versionString = Encoding.UTF8.GetString(vString);
            }
            return true;
        }
    }
}
