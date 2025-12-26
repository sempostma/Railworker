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
    public static class RWUtils
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
                    throw new TSPathInRegistryNotFoundException("Cant find registry variable HKLM\\" + path + " /v \"InstallLocation\".");
                }

                var tsExe = Path.Combine(tsPath, "RailWorks.exe");
                if (!File.Exists(tsExe))
                {
                    throw new TSPathInRegistryNotFoundException("Cant find a valid installation from the regstry.");
                }

                return tsPath;
            } 
            else
            {
                throw new TSPathInRegistryNotFoundException("Cant find TS Path in registry because we are not running Windows.");
            }
        }

        public static IEnumerable<T> CatchExceptions<T>(this IEnumerable<T> src, Action<Exception> action = null)
        {
            using (var enumerator = src.GetEnumerator())
            {
                bool next = true;

                while (next)
                {
                    try
                    {
                        next = enumerator.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        if (action != null)
                            action(ex);
                        continue;
                    }

                    if (next)
                        yield return enumerator.Current;
                }
            }
        }
    }
}
