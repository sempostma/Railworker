namespace RWLib.Tracks
{
    public class RWRouteCoord
    {
        public int XRouteCoord { get; internal set; }
        public int ZRouteCoord { get; internal set; }

        public double XTileCoord { get; internal set; }
        public double ZTileCoord { get; internal set; }

        public double X { get => XRouteCoord * 1024 + XTileCoord; }
        public double Z { get => ZRouteCoord * 1024 + ZTileCoord; }

        public static RWRouteCoord FromAbsoluteCoords(double x, double z)
        {
            var result = new RWRouteCoord
            {
                XRouteCoord = (int)Math.Floor(x / 1024.0),
                XTileCoord = (x % 1024),

                ZRouteCoord = (int)Math.Floor(z / 1024.0),
                ZTileCoord = (z % 1024),
            };

            if (x < 0) result.XTileCoord += 1024;
            if (z < 0) result.ZTileCoord += 1024;

            return result;
        }
    }
}