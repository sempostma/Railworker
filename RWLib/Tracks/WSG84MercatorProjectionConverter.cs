using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWLib.Tracks
{
    public class WSG84MercatorProjectionConverter
    {
        private double referenceLatitude;
        private double referenceLongitude;

        private int zone;

        public WSG84MercatorProjectionConverter(double referenceLatitude, double referenceLongitude, int zone)
        {
            this.referenceLatitude = referenceLatitude;
            this.referenceLongitude = referenceLongitude;
            this.zone = zone;
        }

        public (double, double) ConvertToLatitudeAndLongitude(double x, double y)
        {
            var f = new CoordinateTransformationFactory();
            var utm = ProjectedCoordinateSystem.WGS84_UTM(zone, referenceLongitude > 0);
            var wsg84 = GeographicCoordinateSystem.WGS84;

            var toLatLong = f.CreateFromCoordinateSystems(utm, wsg84);
            var toUtm = f.CreateFromCoordinateSystems(wsg84, utm);

            (double xRef, double yRef) = toUtm.MathTransform.Transform(referenceLongitude, referenceLatitude);

            var newX = xRef + x;
            var newY = yRef + y;

            (double longitude, double latitude) = toLatLong.MathTransform.Transform(newX, newY);

            return (latitude, longitude);
        }
    }
}
