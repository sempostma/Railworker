using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RWLib
{
    public abstract class RWLibraryDependent
    {
        protected RWLibrary rWLib;

        internal RWLibraryDependent(RWLibrary rWLib)
        {
            this.rWLib = rWLib;
        }

        public async Task<XDocument> LoadXMLSafe(String filename)
        {
            try
            {
                using (StreamReader sr = new StreamReader(filename))
                {
                    XDocument xDocument = await XDocument.LoadAsync(sr, LoadOptions.None, CancellationToken.None);
                    return xDocument;
                }
            }
            catch (XmlException)
            {
                rWLib.options.Logger.Log(RWLogType.Error, $"Malformed XML in \"{filename}\"");
            }

            string xml = File.ReadAllText(filename, Encoding.UTF8);
            return ParseXMLSafe(xml);
        }

        public XDocument ParseXMLSafe(String xml)
        {
            xml = Regex.Replace(
                xml,
                @"[^\u0009\u000A\u000D\u0020-\uD7FF\uE000-\uFFFD\u10000-\u10FFFF]",
                string.Empty);

            return XDocument.Parse(xml);
        }
    }
}
