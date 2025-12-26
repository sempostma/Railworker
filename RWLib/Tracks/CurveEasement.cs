using System;
using System.Data;
using System.Drawing;

namespace RWLib.Tracks
{

    // this is a euler spiral. 
    // An Euler spiral, also known as a clothoid or a transition curve, is a curve whose curvature changes linearly with its arc length. The formula for the curvature (k)
    // k(s) = 1/R = A * s
    // where k(s) is the curvature at arc length s
    // R is the radius of curvature at a specific point on the curve
    // A is the rate of curvature change (constant)
    // s is the arc length along the curve

    // To find the end point of an Euler spiral given a starting point, starting angle, and a certain distance, you need to integrate the curvature function to find the change in angle, and then use that to calculate the coordinates.

    public class CurveEasement : TrackCurve
    {
        public struct CurveEasementPoint
        {
            public double X;
            public double Z;
            public double Angle;
        }

        public int Sign { get; set; }
        public double Sharpness { get; internal set; }
        public double Offset { get; internal set; }
        public bool OffsetIsZero {  get; internal set; }
        public int TraversalSign { get; internal set; }

        // L0 is often the same as length, meaning that L0Tangent1, L0Tangent2 and L0Atan2 are the referencing to the end point of the clothoid.
        // But sometimes length is shorter than L0. When the traversal sign is -1, it goes from a straight section to a curved section.
        // If the length is shorter it means that its still going from straight to curved but the start is somewhere on the clothoid instead of at the beginning of it.
        // To make it easier to think about, the start of the clothoid is the beginning, straight section of a eural spiral. If the length < L0 and the Traversal sign is -1 it means 
        // that we are still traversing inwords into the spiral but we are not starting from the beginning but from L0 - length.
        public double L0 {  get; internal set; }
        public (double, double, double, double) L0Offset { get; internal set; }
        public double L0Tangent1 { get; internal set; }
        public double L0Tangent2 { get; internal set; }
        public double L0Atan2 { get => Math.Atan2(L0Tangent2, L0Tangent1); }
        
        public double GetCurvatureAtLength(double length)
        {
            // traversal sign = 0 means its starting arced and is heading to linearity
            // traversal sign = 1 means its starting straight and is heading to curved
            // the higher the length the more curved the section is so we are starting arced (traversal sign = 0) we should subtract it from the full length to get the inverse effect.
            // (requires some thinking but once you get it its actually very intuative, just read up on the euler spiral, the math is pretty easy, its just a gradual angle increase)

            if (TraversalSign == 1) length = L0 + length;
            else length = L0 - length;
            return Sharpness * length;
        }

        public double StartingCurvature { get => GetCurvatureAtLength(0); }
        public double EndCurvature { get => GetCurvatureAtLength(Length); }

        public double StartSweepAngle { get => StartingCurvature * Length; }
        public double EndSweepAngle { get => EndCurvature * Length; }

        public Lazy<List<CurveEasementPoint>> Points {
            get => new Lazy<List<CurveEasementPoint>>(() =>
            {
                var list = new List<CurveEasementPoint>();

                var angle = Atan2;
                double x = 0;
                double z = 0;
                double curvature;

                int i;
                for (i = 0; i < Length; i++)
                {
                    curvature = GetCurvatureAtLength(i);
                    angle += curvature * -Sign * TraversalSign;
                    x += Math.Cos(angle);
                    z += Math.Sin(angle);

                    list.Add(new CurveEasementPoint { Angle = angle, X = x, Z = z });
                }

                return list;
            });
        }

        public RWRouteCoord EstimatePositionAt(double length)
        {
            int index = (int)Math.Floor(length);
            if (index >= Points.Value.Count - 1) index = Points.Value.Count - 1;
            var lastPoint = Points.Value[index];

            double fraction = length - index;

            var curvature = GetCurvatureAtLength(index);
            var angle = lastPoint.Angle + curvature * fraction * -Sign * TraversalSign;
            var x = lastPoint.X + Math.Cos(angle);
            var z = lastPoint.Z + Math.Sin(angle);

            return RWRouteCoord.FromAbsoluteCoords(Position.X + x, Position.Z + z);
        }

        //public RWRouteCoord GetPositionAt(double length)
        //{
        //    if (TraversalSign == -1) return GetPositionAtInverseTraversal(length);

        //    var start = new Clothoid.Geometry.Point(Position.X, Position.Z);

        //    var tangent1 = Math.Cos(Atan2);
        //    var tangent2 = Math.Sin(Atan2);

        //    var startDirection = new UnitVector(tangent1, tangent2);
        //    var clothoid = new Clothoid.Geometry.Clothoid(Sharpness * Length * -Sign * 1.3, start, startDirection, 0, length);
        //    var endPoint = clothoid.EndPoint;

        //    return RWRouteCoord.FromAbsoluteCoords(endPoint.X, endPoint.Y);
        //}

        //private RWRouteCoord GetPositionAtInverseTraversal(double length)
        //{
        //    length = Length - length;
        //    var start = new Clothoid.Geometry.Point(Position.X, Position.Z);

        //    var endAngle = Sharpness * Length * Length / 2 * -Sign * 1.3;
        //    var tangent1 = Math.Cos(Atan2 - endAngle);
        //    var tangent2 = Math.Sin(Atan2 - endAngle);

        //    var startDirection = new UnitVector(tangent1, tangent2).Rotate(Math.PI);
        //    var clothoidEnd = new Clothoid.Geometry.Clothoid(Sharpness * Length * -Sign, Clothoid.Geometry.Point.Zero, startDirection, 0, Length);
        //    var endPointEnd = clothoidEnd.EndPoint;

        //    start -= new Vector(endPointEnd.X, endPointEnd.Y);
        //    var clothoid = new Clothoid.Geometry.Clothoid(Sharpness * Length * -Sign, start, startDirection, 0, length);
        //    var endPoint = clothoid.EndPoint;

        //    return RWRouteCoord.FromAbsoluteCoords(endPoint.X, endPoint.Y);
        //}
    }
}