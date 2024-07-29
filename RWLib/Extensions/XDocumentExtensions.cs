using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RWLib.Extensions
{
    public static class XDocumentExtensions
    {
        public static long GetFastDeepConsistentHash(this XDocument doc)
        {
            unchecked // Allow arithmetic overflow, wrap around
            {
                long hash = 17;
                ComputeHash(doc.Root!, ref hash);
                return hash;
            }
        }

        private static void ComputeHash(XElement element, ref long hash)
        {
            if (element == null)
            {
                return;
            }

            // Combine hash with element name
            hash = hash * 31 + element.Name.ToString().GetHashCode();

            // Combine hash with attributes
            foreach (var attr in element.Attributes())
            {
                hash = hash * 31 + attr.Name.ToString().GetHashCode();
                hash = hash * 31 + attr.Value.GetConsistentHash();
            }

            // Recursively combine hash with child elements
            foreach (var node in element.Nodes())
            {
                if (node is XElement e)
                {
                    ComputeHash(e, ref hash);
                }
                else if (node is XText t)
                {
                    hash = hash * 31 + t.Value.GetConsistentHash();
                }
            }
        }
    }
}
