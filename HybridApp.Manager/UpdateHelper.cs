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
        public static bool CheckForUpdate(int iCurrentVersion)
        {
            using (WebClient wc = new WebClient())
            {
                byte[] vString = wc.DownloadData(new Uri("https://raw.githubusercontent.com/OldUser101/HybridApp/refs/heads/master/HybridApp.Update/LATEST_VS.txt"));
                string versionString = Encoding.UTF8.GetString(vString);
                int iVersion = Convert.ToInt32(versionString);

                if (iVersion > iCurrentVersion) 
                {
                    return true;
                }
            }
            return false;
        }

        public static bool DownloadUpdate(string target)
        {
            using (WebClient wc = new WebClient())
            {
                byte[] fPath = wc.DownloadData("https://raw.githubusercontent.com/OldUser101/HybridApp/refs/heads/master/HybridApp.Update/LATEST_REL.txt");
                string sFileTarget = Encoding.UTF8.GetString(fPath);
                string targetFile = sFileTarget.Split('\n')[IntPtr.Size == 8 ? 1 : 0];

                wc.DownloadFile(targetFile, target);

                if (!System.IO.File.Exists(target))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
