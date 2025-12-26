using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailworkerMegaFreightPack1
{
    internal static class Helpers
    {
        public static string Filename(this string str)
        {
            return str
                .Replace('-', '_')
                .Replace(' ', '_')
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace("\t", "");
        }
    }
}
