using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Railworker.Pages
{
    /// <summary>
    /// Static class to store shared directory paths across different pages
    /// </summary>
    public static class SharedDirectories
    {
        /// <summary>
        /// Last directory used for JSON files
        /// </summary>
        public static string LastJsonDirectory { get; set; } = "";
        
        /// <summary>
        /// Last directory used for texture files
        /// </summary>
        public static string LastTextureDirectory { get; set; } = "";
    }
}
