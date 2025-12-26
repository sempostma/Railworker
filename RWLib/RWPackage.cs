using System.IO.Compression;

namespace RWLib
{
    public class RWPackage
    {
        public ZipArchive Archive { get; internal set; }
        public RWPackageInfoWithMD5 PackageInfo { get; internal set; }
    }
}