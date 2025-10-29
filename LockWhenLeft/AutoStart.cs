using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LockWhenLeft
{
    // Create a shortcut file in the current users start up folder
    // Based on this answer on Stackoverflow:
    // http://stackoverflow.com/a/19914018/198065
    public class AutoStart
    {
        public bool IsEnabled
        {
            get { return HasShortcut(); }

            set
            {
                var appLink = GetAppLinkPath();

                if (value)
                {
                    CreateShortcut(appLink);
                }
                else if (IsEnabled)
                {
                    DeleteShortcut(appLink);
                }
            }
        }

        private static bool HasShortcut()
        {
            try
            {
                return File.Exists(GetAppLinkPath());
            }
            catch
            {
                return false;
            }
        }

        private static string GetAppLinkPath()
        {
            var appDataStart =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Windows\Start Menu\Programs\Startup");
            var appLink = Path.Combine(appDataStart, "LockWhenLeft.lnk");
            return appLink;
        }

        private static void DeleteShortcut(string appLink)
        {
            try
            {
                File.Delete(appLink);
            }
            catch
            {
                throw new AutoStartException(
                    "It was not possible to delete the shortcut to LockWhenLeft in the startup folder");
            }
        }

        private static void CreateShortcut(string appLink)
        {
            try
            {
                var exeLocation = Environment.ProcessPath;

                //Windows Script Host Shell Object
                var t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8"));
                dynamic shell = Activator.CreateInstance(t);
                try
                {
                    var lnk = shell.CreateShortcut(appLink);
                    try
                    {
                        lnk.TargetPath = exeLocation;
                        lnk.WorkingDirectory = new FileInfo(exeLocation).DirectoryName;
                        lnk.Save();
                    }
                    finally
                    {
                        Marshal.FinalReleaseComObject(lnk);
                    }
                }
                finally
                {
                    Marshal.FinalReleaseComObject(shell);
                }
            }
            catch
            {
                throw new AutoStartException(
                    "It was not possible to create a shortcut to LockWhenLeft in the startup folder");
            }
        }
    }

    public class AutoStartException : Exception
    {
        public AutoStartException(string message)
            : base(message)
        {
        }
    }
}