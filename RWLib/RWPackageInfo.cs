
namespace RWLib
{
    public class RWPackageInfo
    {
        public enum LicenseType { Unlicensed = 0, Unprotected = 1, Protected = 2 }

        public required string Name { get; set; }
        public required LicenseType License { get; set; }
        public required string Author { get; set; }
        public required string[] FileNames { get; set; }

        public int GetZipOffset()
        {
            return Author.Length + 1;
        }

        public string ToFilename()
        {
            return Name.Replace(' ', '_') + ".rwp";
        }
    }
}   