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
    }
}
