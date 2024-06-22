
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGGRSSCOntainerPackGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            RWLibrary rwLib = new RWLibrary(new RWLibOptions { UseCustomSerz = true, Logger = new Logger() });
        }
    }
}
