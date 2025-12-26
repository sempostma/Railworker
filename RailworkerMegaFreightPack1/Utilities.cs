using RWLib.Interfaces;
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
        public class Logger : IRWLogger
        {
            public void Log(RWLogType type, string message)
            {
                Console.WriteLine($"[{type}] {message}");
            }
        }

        public static Logger ConsoleLogger = new Logger();

        public static String ReadFile(String embeddedResource)
        {
            var stream = OpenFile(embeddedResource);
            using (StreamReader reader = new StreamReader(stream))
            {
                string file = reader.ReadToEnd(); //Make string equal to full file
                return file;
            }
        }

        public static Stream OpenFile(String embeddedResource)
        {
            var assembly = Assembly.GetAssembly(typeof(RailworkerMegaFreightPack1.Utilities))!;
            var files = assembly.GetManifestResourceNames();
            var resource = "RailworkerMegaFreightPack1.Resources." + embeddedResource;
            var stream = assembly.GetManifestResourceStream(resource);
            if (stream == null) throw new FileNotFoundException($"Unable to get embedded resource {resource}. All files: ${files.ToArray()}");
            return stream;
        }

        public static IEnumerable<string> FindResources(string folderPrefix)
        {
            var assembly = Assembly.GetAssembly(typeof(RailworkerMegaFreightPack1.Utilities))!;
            return assembly.GetManifestResourceNames()
                           .Select(x => String.Join(".", x.Split(".").Skip(2)))
                           .Where(r => r.StartsWith(folderPrefix));
        }
    }
}
