using RWLib;

namespace Railworker.Core
{
    internal class AppGlobals
    {
        public Logger Logger { get; set; }

        internal AppGlobals(Logger logger)
        {
            Logger = logger;
        }
    }
}