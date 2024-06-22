using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RailworkerMegaFreightPack1
{
    public static class Utilities
    {
        public static String ReadFile(String embeddedResource)
        {
            var assembly = Assembly.GetAssembly(typeof(RailworkerMegaFreightPack1.Utilities))!;
            var files = assembly.GetManifestResourceNames();
            var resource = "RailworkerMegaFreightPack1.Resources." + embeddedResource;
            var stream = assembly.GetManifestResourceStream(resource);
            if (stream == null) throw new FileNotFoundException($"Unable to get embedded resource {resource}. All files: ${files.ToArray()}");
            using (StreamReader reader = new StreamReader(stream))
            {
                string file = reader.ReadToEnd(); //Make string equal to full file
                return file;
            }
        }
    }
}
