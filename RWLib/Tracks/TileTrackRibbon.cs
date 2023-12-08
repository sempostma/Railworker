
namespace RWLib.Tracks
{
    public class TileTrackRibbon
    {
        // the name "Segments" is sometimes used sometimes instead of curves
        public List<TrackCurve> Curves { get; set; } = new List<TrackCurve>();
        public string RibbonID { get; set; } = "";
        public string NetworkTypeId { get; set; } = "";
        public string RouteGuid { get; set; } = "";
    }
}