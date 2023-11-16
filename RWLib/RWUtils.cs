using Microsoft.Win32;
using RWLib.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib
{
    internal static class RWUtils
    {
        public static readonly XNamespace KujuNamspace = "http://www.kuju.com/TnT/2003/Delta";

        internal static async Task CopyFileAsync(string sourceFile, string destinationPath)
        {
            using (Stream source = File.Open(sourceFile, FileMode.Open, FileAccess.Read)) {
                using (Stream destination = File.Create(destinationPath))
                {
                    await source.CopyToAsync(destination);
                }
            }
        }

        public static string GetTSPathFromSteamAppInRegistry()
        {
            string path = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 24010";

            if (OperatingSystem.IsWindows())
            {
                RegistryKey? key = Registry.LocalMachine.OpenSubKey(path);
                string? tsPath = key?.GetValue("InstallLocation") as string;

                if (tsPath == null)
                {
                    throw new TSPathInRegistryNotFoundException("Cant find registry variable HKLM\\" + path + " /v \"InstallLocation\"");
                }

                return tsPath;
            } 
            else
            {
                throw new TSPathInRegistryNotFoundException("Cant find TS Path in registry because we are not running Windows.");
            }
        }
    }
}
