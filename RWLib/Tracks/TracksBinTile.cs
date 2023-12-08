
using System.IO.Compression;

namespace RWLib.Tracks
{
    public class TracksBinTile
    {
        public int RouteCoordX { get; set; }
        public int RouteCoordZ { get; set; }
        public string TracksBinFile { get; set; } = "";
        public string RouteGuid { get; set; } = "";
        public ZipArchiveEntry? ZipArchiveEntry { get; internal set; }
    }
}