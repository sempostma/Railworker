namespace RWLib.Tracks
{
    public class CurveArc : TrackCurve
    {
        public double Curvature { get; internal set; }
        public int Sign { get; internal set; }

        public double Radius { get => 1.0 / Curvature; }
        public double SweepAngle { get => Length * Curvature * -Sign; }

        public RWRouteCoord GetReferenceCircleCenter() { 
        
            var x = Position.X + Radius * Math.Cos(Atan2 + Math.PI / 2) * -Sign;
            var z = Position.Z + Radius * Math.Sin(Atan2 + Math.PI / 2) * -Sign;

            var result = RWRouteCoord.FromAbsoluteCoords(x, z);
            return result;
        }

        public RWRouteCoord GetPositionAt(double distance)
        {
            var referenceCenter = GetReferenceCircleCenter();

            var angle = distance * Curvature * -Sign;

            var x = referenceCenter.X + Radius * Math.Cos(Atan2 + angle - Math.PI / 2) * -Sign;
            var z = referenceCenter.Z + Radius * Math.Sin(Atan2 + angle - Math.PI / 2) * -Sign;

            var result = RWRouteCoord.FromAbsoluteCoords(x, z);

            return result;
        }

        public RWRouteCoord GetEndPosition()
        {
            return GetPositionAt(Length);
        }
    }
}