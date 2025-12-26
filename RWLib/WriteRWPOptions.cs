namespace RWLib
{
    public class WriteRWPOptions
    {
        /// <summary>
        /// When true, the RWP writer will bundle the selected files into one or more embedded .ap (STORE zip) archives
        /// inside the RWP (after the normal RWP header bytes). Each .ap archive groups files by their
        /// Provider/Product (i.e. first two path segments under Assets). This flag does NOT alter or depend on
        /// licensing / protection; it only changes the internal storage format from loose files to grouped .ap archives.
        /// </summary>
        public bool PackageAsAPAsset { get; set; } = false;
    }
}