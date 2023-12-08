using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace RWLib.Tracks
{

    public class TrackCurve
    {
        public int Id { get; set; }
        public double Length { get; set; }
        public double Tangent1 { get; set; }
        public double Tangent2 { get; set; }
        public RWRouteCoord Position { get; set; } = new RWRouteCoord();

        public double Atan2 { get => Math.Atan2(Tangent2, Tangent1); }
    }
}