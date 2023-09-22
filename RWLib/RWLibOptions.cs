using System.Diagnostics;

namespace RWLib
{
    public class RWLibOptions
    {
        public string TSPath { get; set; } = String.Empty;
        public IRWLogger logger { get; set; }

        public RWLibOptions(IRWLogger logger)
        {
            this.logger = logger;
        }
    }
}