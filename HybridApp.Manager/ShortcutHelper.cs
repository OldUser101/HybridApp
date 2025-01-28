using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IWshRuntimeLibrary;

namespace HybridApp.Manager
{
    public class ShortcutHelper
    {
        public static bool CreateShortcut(string shortcutPath, string targetPath, string iconPath, string args)
        {
            try
            {
                WshShell shell = new WshShell();

                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(iconPath))
                {
                    shortcut.IconLocation = iconPath;
                }
                shortcut.Arguments = args;
                shortcut.Save();

            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

    }
}
